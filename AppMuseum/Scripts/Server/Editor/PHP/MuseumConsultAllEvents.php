<?php
	include 'ConfigurationUserManagement.php';

	$link = $GLOBALS['LINK_DATABASE'];
	if (function_exists('mysqli_set_charset')) { @mysqli_set_charset($link, 'utf8mb4'); }

	$mode = isset($_GET['mode']) ? $_GET['mode'] : 'meta';

	switch ($mode) {
		case 'meta':           respond(meta_stats($link, base_filters($_GET)));            break;
		case 'poi_engagement': respond(poi_engagement($link, base_filters($_GET)));        break;
		case 'funnel':         respond(funnel($link, base_filters($_GET)));                break;
		case 'ai_questions':   respond(ai_questions($link, base_filters($_GET), $_GET));   break;
		case 'feed':           respond(event_feed($link, base_filters($_GET), $_GET));     break;
		default:
			http_response_code(400);
			respond(array('error' => 'Unknown mode: ' . $mode));
	}

	mysqli_close($link);


	/* ============================ helpers ============================ */

	function respond($data) {
		echo json_encode($data, JSON_UNESCAPED_UNICODE);
	}

	// Common WHERE parts shared by every mode. Returns [conditions[], types, params[]].
	function base_filters($q) {
		$cond = array(); $types = ''; $params = array();
		if (isset($q['from'])     && $q['from']     !== '') { $cond[] = 'date >= ?';   $types .= 'i'; $params[] = (int)$q['from']; }
		if (isset($q['to'])       && $q['to']       !== '') { $cond[] = 'date <= ?';   $types .= 'i'; $params[] = (int)$q['to']; }
		if (isset($q['email'])    && $q['email']    !== '') { $cond[] = 'email = ?';   $types .= 's'; $params[] = $q['email']; }
		if (isset($q['age'])      && $q['age']      !== '') { $cond[] = 'age = ?';     $types .= 'i'; $params[] = (int)$q['age']; }
		if (isset($q['language']) && $q['language'] !== '') { $cond[] = 'language = ?';$types .= 's'; $params[] = $q['language']; }
		if (isset($q['area'])     && $q['area']     !== '') { $cond[] = 'level = ?';   $types .= 'i'; $params[] = (int)$q['area']; }
		return array($cond, $types, $params);
	}

	function where_clause($cond) {
		return count($cond) ? ('WHERE ' . implode(' AND ', $cond)) : '';
	}

	function clamp_limit($v, $def, $max) {
		$n = ($v === null || $v === '') ? $def : (int)$v;
		if ($n < 1)   $n = 1;
		if ($n > $max) $n = $max;
		return $n;
	}

	// Prepare + bind (by reference) + execute. Returns [stmt, result].
	function run_query($link, $sql, $types, $params) {
		$stmt = mysqli_prepare($link, $sql);
		if (!$stmt) {
			http_response_code(500);
			respond(array('error' => 'prepare failed', 'db' => mysqli_error($link)));
			exit;
		}
		if ($types !== '') {
			$bind = array($stmt, $types);
			for ($i = 0; $i < count($params); $i++) { $bind[] = &$params[$i]; }
			call_user_func_array('mysqli_stmt_bind_param', $bind);
		}
		mysqli_stmt_execute($stmt);
		return array($stmt, mysqli_stmt_get_result($stmt));
	}

	// Flatten {"Items":[{parameter,type,value}, ...]} into [parameter => value].
	function items_map($data_json) {
		$map = array();
		$obj = json_decode($data_json, true);
		if (is_array($obj) && isset($obj['Items']) && is_array($obj['Items'])) {
			foreach ($obj['Items'] as $it) {
				if (isset($it['parameter'])) {
					$map[$it['parameter']] = isset($it['value']) ? $it['value'] : null;
				}
			}
		}
		return $map;
	}

	function truthy($v) {
		return in_array(strtolower((string)$v), array('true', '1', 'yes'), true);
	}

	/* ============================ mode: meta ============================ */
	// Headline numbers for the top strip. Pure SQL GROUP BY — no JSON parsing,
	// tiny payload, fast even on a large table (with the indexes below).
	function meta_stats($link, $f) {
		list($cond, $types, $params) = $f;
		$where = where_clause($cond);

		list($stmt, $res) = run_query($link, "SELECT name, COUNT(*) AS c FROM analytics $where GROUP BY name", $types, $params);
		$byName = array(); $total = 0;
		while ($r = mysqli_fetch_object($res)) { $byName[$r->name] = (int)$r->c; $total += (int)$r->c; }
		mysqli_stmt_close($stmt);

		list($stmt2, $res2) = run_query($link, "SELECT COUNT(DISTINCT email) AS users, MIN(date) AS first_ts, MAX(date) AS last_ts FROM analytics $where", $types, $params);
		$row = mysqli_fetch_object($res2);
		mysqli_stmt_close($stmt2);

		return array(
			'mode'           => 'meta',
			'totalEvents'    => $total,
			'byName'         => $byName,
			'uniqueVisitors' => (int)$row->users,
			'firstEvent'     => $row->first_ts !== null ? (int)$row->first_ts : null,
			'lastEvent'      => $row->last_ts  !== null ? (int)$row->last_ts  : null
		);
	}


	/* ====================== mode: poi_engagement ====================== */
	// Streams the filtered POI-Visited / POI-Replayed rows and aggregates them
	// incrementally, so PHP memory stays O(number of POIs), not O(rows). The
	// browser receives one small row per POI.
	function poi_engagement($link, $f) {
		list($cond, $types, $params) = $f;
		$cond[] = "name IN ('POI-Visited','POI-Replayed')";   // literal -> no user input
		list($stmt, $res) = run_query($link, "SELECT name, data FROM analytics " . where_clause($cond), $types, $params);

		$poi = array();
		while ($r = mysqli_fetch_object($res)) {
			$m = items_map($r->data);
			if (!isset($m['number'])) continue;
			$n = (int)$m['number'];
			if (!isset($poi[$n])) {
				$poi[$n] = array('visits'=>0,'listenSum'=>0.0,'listenN'=>0,'skips'=>0,
								 'skipSum'=>0.0,'skipN'=>0,'pausedSum'=>0,'restartedSum'=>0,'replays'=>0);
			}
			$p =& $poi[$n];

			if ($r->name === 'POI-Replayed') { $p['replays']++; unset($p); continue; }

			$p['visits']++;
			if (isset($m['started'], $m['ended'])) {
				$dur = (float)$m['ended'] - (float)$m['started'];
				if ($dur >= 0) { $p['listenSum'] += $dur; $p['listenN']++; }
			}
			if (isset($m['skipped']) && truthy($m['skipped'])) {
				$p['skips']++;
				if (isset($m['skiptime']) && (float)$m['skiptime'] >= 0) {
					$p['skipSum'] += (float)$m['skiptime']; $p['skipN']++;
				}
			}
			if (isset($m['paused']))    $p['pausedSum']    += (int)$m['paused'];
			if (isset($m['restarted'])) $p['restartedSum'] += (int)$m['restarted'];
			unset($p);
		}
		mysqli_stmt_close($stmt);

		$rows = array();
		foreach ($poi as $n => $p) {
			$rows[] = array(
				'poi'          => $n,
				'visits'       => $p['visits'],
				'avgListen'    => $p['listenN'] ? round($p['listenSum'] / $p['listenN'], 1) : null,
				'skipRate'     => $p['visits']  ? round($p['skips'] / $p['visits'], 3) : null,
				'avgSkipTime'  => $p['skipN']   ? round($p['skipSum'] / $p['skipN'], 1) : null,
				'avgPaused'    => $p['visits']  ? round($p['pausedSum'] / $p['visits'], 2) : null,
				'avgRestarted' => $p['visits']  ? round($p['restartedSum'] / $p['visits'], 2) : null,
				'replays'      => $p['replays']
			);
		}
		usort($rows, function ($a, $b) { return $a['poi'] - $b['poi']; });
		return array('mode' => 'poi_engagement', 'pois' => $rows);
	}


	/* ============================ mode: funnel ============================ */
	// "How far did visitors get?" We track the furthest POI number each visitor
	// reached (email as a session proxy), then reached(k) = visitors whose max >= k.
	function funnel($link, $f) {
		list($cond, $types, $params) = $f;

		$condV = $cond; $condV[] = "name = 'POI-Visited'";
		list($stmt, $res) = run_query($link, "SELECT email, data FROM analytics " . where_clause($condV), $types, $params);
		$maxReached = array(); $globalMax = 0;
		while ($r = mysqli_fetch_object($res)) {
			$m = items_map($r->data);
			if (!isset($m['number'])) continue;
			$n = (int)$m['number'];
			if (!isset($maxReached[$r->email]) || $n > $maxReached[$r->email]) $maxReached[$r->email] = $n;
			if ($n > $globalMax) $globalMax = $n;
		}
		mysqli_stmt_close($stmt);

		$condS = $cond; $condS[] = "name = 'EventLevelStart'";
		list($stmt2, $res2) = run_query($link, "SELECT COUNT(DISTINCT email) AS starts FROM analytics " . where_clause($condS), $types, $params);
		$starts = (int)mysqli_fetch_object($res2)->starts;
		mysqli_stmt_close($stmt2);

		// Histogram of "furthest reached", then suffix-sum -> reached(k). O(visitors + maxPOI).
		$steps = array();
		if ($globalMax > 0) {
			$hist = array_fill(1, $globalMax, 0);
			foreach ($maxReached as $mx) { if ($mx >= 1) $hist[$mx]++; }
			$suffix = 0; $reachedAt = array();
			for ($k = $globalMax; $k >= 1; $k--) { $suffix += $hist[$k]; $reachedAt[$k] = $suffix; }
			for ($k = 1; $k <= $globalMax; $k++) { $steps[] = array('poi' => $k, 'reached' => $reachedAt[$k]); }
		}
		return array('mode' => 'funnel', 'starts' => $starts, 'steps' => $steps);
	}


	/* ======================= mode: ai_questions ======================= */
	// The one raw dashboard. Keyset pagination by id (pass the last id back as
	// `before` for the next page) — stays fast no matter how deep you scroll.
	function ai_questions($link, $f, $q) {
		list($cond, $types, $params) = $f;
		$cond[] = "name = 'AI-Question'";
		if (isset($q['search']) && $q['search'] !== '') { $cond[] = 'data LIKE ?'; $types .= 's'; $params[] = '%' . $q['search'] . '%'; }
		if (isset($q['before']) && $q['before'] !== '') { $cond[] = 'id < ?';      $types .= 'i'; $params[] = (int)$q['before']; }

		$limit = clamp_limit(isset($q['limit']) ? $q['limit'] : null, 50, 200);
		$sql = "SELECT id, email, age, language, level, date, data FROM analytics " . where_clause($cond) . " ORDER BY id DESC LIMIT ?";
		$types .= 'i'; $params[] = $limit;
		list($stmt, $res) = run_query($link, $sql, $types, $params);

		$items = array(); $lastId = null;
		while ($r = mysqli_fetch_object($res)) {
			$m = items_map($r->data);
			$items[] = array(
				'id'       => (int)$r->id,
				'date'     => (int)$r->date,
				'email'    => $r->email,
				'age'      => (int)$r->age,
				'language' => $r->language,
				'area'     => (int)$r->level,
				'question' => clean_question(isset($m['question']) ? $m['question'] : ''),
				'answer'   => isset($m['answer']) ? $m['answer'] : ''
			);
			$lastId = (int)$r->id;
		}
		mysqli_stmt_close($stmt);
		return array('mode' => 'ai_questions', 'items' => $items, 'limit' => $limit, 'nextBefore' => $lastId);
	}

	// The app prepends a fixed "rules" preamble to every question; keep only the
	// visitor's actual question. Tune the marker to match your exact preamble.
	function clean_question($text) {
		$marker = 'outside of this field.';
		$pos = strripos($text, $marker);
		if ($pos !== false) $text = substr($text, $pos + strlen($marker));
		return trim($text);
	}


	/* ============================ mode: feed ============================ */
	// Generic paginated raw feed for the "recent events" list under the dashboards.
	function event_feed($link, $f, $q) {
		list($cond, $types, $params) = $f;
		if (isset($q['name'])   && $q['name']   !== '') { $cond[] = 'name = ?'; $types .= 's'; $params[] = $q['name']; }
		if (isset($q['before']) && $q['before'] !== '') { $cond[] = 'id < ?';   $types .= 'i'; $params[] = (int)$q['before']; }

		$limit = clamp_limit(isset($q['limit']) ? $q['limit'] : null, 50, 200);
		$sql = "SELECT id, name, email, age, language, level, date, data FROM analytics " . where_clause($cond) . " ORDER BY id DESC LIMIT ?";
		$types .= 'i'; $params[] = $limit;
		list($stmt, $res) = run_query($link, $sql, $types, $params);

		$items = array(); $lastId = null;
		while ($r = mysqli_fetch_object($res)) {
			$items[] = array(
				'id'    => (int)$r->id, 'name' => $r->name, 'date' => (int)$r->date,
				'email' => $r->email, 'age' => (int)$r->age, 'language' => $r->language, 'area' => (int)$r->level,
				'params' => items_map($r->data)
			);
			$lastId = (int)$r->id;
		}
		mysqli_stmt_close($stmt);
		return array('mode' => 'feed', 'items' => $items, 'limit' => $limit, 'nextBefore' => $lastId);
	}
?>