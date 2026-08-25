using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
public class SymSpell
{
    public enum Verbosity
    {
        Top,
        Closest,
        All
    };

    const int defaultMaxEditDistance = 2;
    const int defaultPrefixLength = 7;
    const int defaultCountThreshold = 1;
    const int defaultInitialCapacity = 16;
    const int defaultCompactLevel = 5;
    const char[] defaultSeparatorChars = (char[])null;

    private readonly int initialCapacity;
    private readonly int maxDictionaryEditDistance;
    private readonly int prefixLength;
    private readonly Int64 countThreshold;
    private readonly uint compactMask;
    private readonly EditDistance.DistanceAlgorithm distanceAlgorithm = EditDistance.DistanceAlgorithm.DamerauOSA;
    private int maxDictionaryWordLength;

    private Dictionary<int, string[]> deletes;
    private readonly Dictionary<string, Int64> words;
    private Dictionary<string, Int64> belowThresholdWords = new Dictionary<string, long>();

    public class SuggestItem : IComparable<SuggestItem>
    {
        public string term = "";
        public int distance = 0;
        public Int64 count = 0;

        public SuggestItem()
        {
        }
        public SuggestItem(string term, int distance, Int64 count)
        {
            this.term = term;
            this.distance = distance;
            this.count = count;
        }
        public int CompareTo(SuggestItem other)
        {
            if (this.distance == other.distance) return other.count.CompareTo(this.count);
            return this.distance.CompareTo(other.distance);
        }
        public override bool Equals(object obj)
        {
            return Equals(term, ((SuggestItem)obj).term);
        }

        public override int GetHashCode()
        {
            return term.GetHashCode();
        }
        public override string ToString()
        {
            return "{" + term + ", " + distance + ", " + count + "}";
        }

        public SuggestItem ShallowCopy()
        {
            return (SuggestItem)MemberwiseClone();
        }
    }

    public int MaxDictionaryEditDistance { get { return this.maxDictionaryEditDistance; } }

    public int PrefixLength { get { return this.prefixLength; } }

    public int MaxLength { get { return this.maxDictionaryWordLength; } }

    public long CountThreshold { get { return this.countThreshold; } }

    public int WordCount { get { return this.words.Count; } }

    public int EntryCount { get { return this.deletes.Count; } }

    public SymSpell(int initialCapacity = defaultInitialCapacity, int maxDictionaryEditDistance = defaultMaxEditDistance
        , int prefixLength = defaultPrefixLength, int countThreshold = defaultCountThreshold
        , byte compactLevel = defaultCompactLevel)
    {
        if (initialCapacity < 0) throw new ArgumentOutOfRangeException(nameof(initialCapacity));
        if (maxDictionaryEditDistance < 0) throw new ArgumentOutOfRangeException(nameof(maxDictionaryEditDistance));
        if (prefixLength < 1 || prefixLength <= maxDictionaryEditDistance) throw new ArgumentOutOfRangeException(nameof(prefixLength));
        if (countThreshold < 0) throw new ArgumentOutOfRangeException(nameof(countThreshold));
        if (compactLevel > 16) throw new ArgumentOutOfRangeException(nameof(compactLevel));

        this.initialCapacity = initialCapacity;
        this.words = new Dictionary<string, Int64>(initialCapacity);
        this.maxDictionaryEditDistance = maxDictionaryEditDistance;
        this.prefixLength = prefixLength;
        this.countThreshold = countThreshold;
        if (compactLevel > 16) compactLevel = 16;
        this.compactMask = (uint.MaxValue >> (3 + compactLevel)) << 2;
    }

    public bool CreateDictionaryEntry(string key, Int64 count, SuggestionStage staging = null)
    {
        if (count <= 0)
        {
            if (this.countThreshold > 0) return false;
            count = 0;
        }
        Int64 countPrevious = -1;

        if (countThreshold > 1 && belowThresholdWords.TryGetValue(key, out countPrevious))
        {
            count = (Int64.MaxValue - countPrevious > count) ? countPrevious + count : Int64.MaxValue;
            if (count >= countThreshold)
            {
                belowThresholdWords.Remove(key);
            }
            else
            {
                belowThresholdWords[key] = count;
                return false;
            }
        }
        else if (words.TryGetValue(key, out countPrevious))
        {
            count = (Int64.MaxValue - countPrevious > count) ? countPrevious + count : Int64.MaxValue;
            words[key] = count;
            return false;
        }
        else if (count < CountThreshold)
        {
            belowThresholdWords[key] = count;
            return false;
        }

        words.Add(key, count);

        if (key.Length > maxDictionaryWordLength) maxDictionaryWordLength = key.Length;

        if (deletes == null) deletes = new Dictionary<int, string[]>(initialCapacity);

        var edits = EditsPrefix(key);
        if (staging != null)
        {
            foreach (string delete in edits) staging.Add(GetStringHash(delete), key);
        }
        else
        {
            foreach (string delete in edits)
            {
                int deleteHash = GetStringHash(delete);
                if (deletes.TryGetValue(deleteHash, out string[] suggestions))
                {
                    var newSuggestions = new string[suggestions.Length + 1];
                    Array.Copy(suggestions, newSuggestions, suggestions.Length);
                    deletes[deleteHash] = suggestions = newSuggestions;
                }
                else
                {
                    suggestions = new string[1];
                    deletes.Add(deleteHash, suggestions);
                }
                suggestions[suggestions.Length - 1] = key;
            }
        }
        return true;
    }

    public Dictionary<string, long> bigrams = new Dictionary<string, long>();
    public long bigramCountMin = long.MaxValue;

    public bool LoadBigramDictionary(string corpus, int termIndex, int countIndex, char[] separatorChars = defaultSeparatorChars)
    {
        if (!File.Exists(corpus)) return false;
        using (Stream corpusStream = File.OpenRead(corpus))
        {
            return LoadBigramDictionary(corpusStream, termIndex, countIndex, separatorChars);
        }
    }
    public bool LoadBigramDictionary(byte[] corpus, int termIndex, int countIndex, char[] separatorChars = defaultSeparatorChars)
    {
        using (Stream corpusStream = new MemoryStream(corpus))
        {
            return LoadBigramDictionary(corpusStream, termIndex, countIndex, separatorChars);
        }
    }

    public bool LoadBigramDictionary(Stream corpusStream, int termIndex, int countIndex, char[] separatorChars = defaultSeparatorChars)
    {
        using (StreamReader sr = new StreamReader(corpusStream, System.Text.Encoding.UTF8, false))
        {
            String line;
            int linePartsLenth = (separatorChars == defaultSeparatorChars) ? 3 : 2;
            while ((line = sr.ReadLine()) != null)
            {
                string[] lineParts = line.Split(separatorChars);

                if (lineParts.Length >= linePartsLenth)
                {
                    string key = (separatorChars == defaultSeparatorChars) ? lineParts[termIndex] + " " + lineParts[termIndex + 1]: lineParts[termIndex];
                    if (Int64.TryParse(lineParts[countIndex], out Int64 count))
                    {
                        bigrams[key] = count;
                        if (count < bigramCountMin) bigramCountMin = count;
                    }
                }
            }
            
        }
        return true;
    }

    public bool LoadDictionary(string corpus, int termIndex, int countIndex, char[] separatorChars = defaultSeparatorChars)
    {
        if (!File.Exists(corpus)) return false;
        using (Stream corpusStream = File.OpenRead(corpus))
        {
            return LoadDictionary(corpusStream, termIndex, countIndex, separatorChars);
        }
    }

    public bool LoadDictionary(byte[] corpus, int termIndex, int countIndex, char[] separatorChars = defaultSeparatorChars)
    {
        using (Stream corpusStream = new MemoryStream(corpus))
        {
            return LoadDictionary(corpusStream, termIndex, countIndex, separatorChars);
        }
    }

    public bool LoadDictionary(Stream corpusStream, int termIndex, int countIndex, char[] separatorChars = defaultSeparatorChars)
    {
        var staging = new SuggestionStage(16384);
        using (StreamReader sr = new StreamReader(corpusStream))
        {
            String line;
            while ((line = sr.ReadLine()) != null)
            {
                string[] lineParts = line.Split(separatorChars);
                if (lineParts.Length >= 2)
                {
                    string key = lineParts[termIndex];
                    if (Int64.TryParse(lineParts[countIndex], out Int64 count))
                    {
                        CreateDictionaryEntry(key, count, staging);
                    }
                }
            }
        }
        CommitStaged(staging);
        return true;
    }

    public bool CreateDictionary(string corpus)
    {
        if (!File.Exists(corpus)) return false;
        using (Stream corpusStream = File.OpenRead(corpus))
        {
            return CreateDictionary(corpusStream);
        }
    }

    public bool CreateDictionary(Stream corpusStream)
    {
        var staging = new SuggestionStage(16384);
        using (StreamReader sr = new StreamReader(corpusStream))
        {
            String line;
            while ((line = sr.ReadLine()) != null)
            {
                foreach (string key in ParseWords(line))
                {
                    CreateDictionaryEntry(key, 1, staging);
                }
            }
        }
        CommitStaged(staging);
        return true;
    }

    public void PurgeBelowThresholdWords()
    {
        belowThresholdWords = new Dictionary<string, long>();
    }

    public void CommitStaged(SuggestionStage staging)
    {
        if (deletes == null) deletes = new Dictionary<int, string[]>(staging.DeleteCount);
        staging.CommitTo(deletes);
    }

    public List<SuggestItem> Lookup(string input, Verbosity verbosity)
    {
        return Lookup(input, verbosity, this.maxDictionaryEditDistance, false);
    }
	
    public List<SuggestItem> Lookup(string input, Verbosity verbosity, int maxEditDistance)
    {
        return Lookup(input, verbosity, maxEditDistance, false);
    }

    public List<SuggestItem> Lookup(string input, Verbosity verbosity, int maxEditDistance, bool includeUnknown)
    {
        if (maxEditDistance > MaxDictionaryEditDistance) throw new ArgumentOutOfRangeException(nameof(maxEditDistance));

        List<SuggestItem> suggestions = new List<SuggestItem>();
        int inputLen = input.Length;
        if (inputLen - maxEditDistance > maxDictionaryWordLength) goto end;

        long suggestionCount = 0;
        if (words.TryGetValue(input, out suggestionCount))
        {
            suggestions.Add(new SuggestItem(input, 0, suggestionCount));
            if (verbosity != Verbosity.All) goto end;
        }

        if (maxEditDistance == 0) goto end;

        HashSet<string> hashset1 = new HashSet<string>();
        HashSet<string> hashset2 = new HashSet<string>();		
        hashset2.Add(input); 

        int maxEditDistance2 = maxEditDistance;
        int candidatePointer = 0;
        var singleSuggestion = new string[1] { string.Empty };
        List<string> candidates = new List<string>();

        int inputPrefixLen = inputLen;
        if (inputPrefixLen > prefixLength)
        {
            inputPrefixLen = prefixLength;
            candidates.Add(input.Substring(0, inputPrefixLen));
        }
        else
        {
            candidates.Add(input);
        }
        var distanceComparer = new EditDistance(this.distanceAlgorithm);
        while (candidatePointer < candidates.Count)
        {
            string candidate = candidates[candidatePointer++];
            int candidateLen = candidate.Length;
            int lengthDiff = inputPrefixLen - candidateLen;

            if (lengthDiff > maxEditDistance2)
            {
                if (verbosity == Verbosity.All) continue;
                break;
            }

            if (deletes.TryGetValue(GetStringHash(candidate), out string[] dictSuggestions))
            {
                for (int i = 0; i < dictSuggestions.Length; i++)
                {
                    var suggestion = dictSuggestions[i];
                    int suggestionLen = suggestion.Length;
                    if (suggestion == input) continue;
                    if ((Math.Abs(suggestionLen - inputLen) > maxEditDistance2)
                        || (suggestionLen < candidateLen)
                        || (suggestionLen == candidateLen && suggestion != candidate))
                        continue;
                    var suggPrefixLen = Math.Min(suggestionLen, prefixLength);
                    if (suggPrefixLen > inputPrefixLen && (suggPrefixLen - candidateLen) > maxEditDistance2) continue;

                    int distance = 0;
                    int min = 0;
                    if (candidateLen == 0)
                    {
                        distance = Math.Max(inputLen, suggestionLen);
                        if (distance > maxEditDistance2 || !hashset2.Add(suggestion)) continue;
                    }
                    else if (suggestionLen == 1)
                    {
                        if (input.IndexOf(suggestion[0]) < 0) distance = inputLen; else distance = inputLen - 1;
                        if (distance > maxEditDistance2 || !hashset2.Add(suggestion)) continue;
                    }
                    else
                    if ((prefixLength - maxEditDistance == candidateLen)
                        && (((min = Math.Min(inputLen, suggestionLen) - prefixLength) > 1)
                            && (input.Substring(inputLen + 1 - min) != suggestion.Substring(suggestionLen + 1 - min)))
                           || ((min > 0) && (input[inputLen - min] != suggestion[suggestionLen - min])
                               && ((input[inputLen - min - 1] != suggestion[suggestionLen - min])
                                   || (input[inputLen - min] != suggestion[suggestionLen - min - 1]))))
                    {
                        continue;
                    }
                    else
                    {
                        if ((verbosity != Verbosity.All && !DeleteInSuggestionPrefix(candidate, candidateLen, suggestion, suggestionLen))
                            || !hashset2.Add(suggestion)) continue;
                        distance = distanceComparer.Compare(input, suggestion, maxEditDistance2);
                        if (distance < 0) continue;
                    }

                    if (distance <= maxEditDistance2)
                    {
                        suggestionCount = words[suggestion];
                        SuggestItem si = new SuggestItem(suggestion, distance, suggestionCount);
                        if (suggestions.Count > 0)
                        {
                            switch (verbosity)
                            {
                                case Verbosity.Closest:
                                    {
                                        if (distance < maxEditDistance2) suggestions.Clear();
                                        break;
                                    }
                                case Verbosity.Top:
                                    {
                                        if (distance < maxEditDistance2 || suggestionCount > suggestions[0].count)
                                        {
                                            maxEditDistance2 = distance;
                                            suggestions[0] = si;
                                        }
                                        continue;
                                    }
                            }
                        }
                        if (verbosity != Verbosity.All) maxEditDistance2 = distance;
                        suggestions.Add(si);
                    }
                }
            }

            if ((lengthDiff < maxEditDistance) && (candidateLen <= prefixLength))
            {
                if (verbosity != Verbosity.All && lengthDiff >= maxEditDistance2) continue;

                for (int i = 0; i < candidateLen; i++)
                {
                    string delete = candidate.Remove(i, 1);

                    if (hashset1.Add(delete)) { candidates.Add(delete); }
                }
            }
        }

        if (suggestions.Count > 1) suggestions.Sort();
		end: if (includeUnknown && (suggestions.Count == 0)) suggestions.Add(new SuggestItem(input, maxEditDistance + 1, 0));																															 
        return suggestions;
    }
	
    public class SuggestionStage
    {
        private struct Node
        {
            public string suggestion;
            public int next;
        }
        private struct Entry
        {
            public int count;
            public int first;
        }
        private Dictionary<int, Entry> Deletes { get; set; }
        private ChunkArray<Node> Nodes { get; set; }

        public SuggestionStage(int initialCapacity)
        {
            Deletes = new Dictionary<int, Entry>(initialCapacity);
            Nodes = new ChunkArray<Node>(initialCapacity * 2);
        }

        public int DeleteCount { get { return Deletes.Count; } }

        public int NodeCount { get { return Nodes.Count; } }

        public void Clear()
        {
            Deletes.Clear();
            Nodes.Clear();
        }
        internal void Add(int deleteHash, string suggestion)
        {
            if (!Deletes.TryGetValue(deleteHash, out Entry entry)) entry = new Entry { count = 0, first = -1 };
            int next = entry.first;
            entry.count++;
            entry.first = Nodes.Count;
            Deletes[deleteHash] = entry;
            Nodes.Add(new Node { suggestion = suggestion, next = next });
        }
        internal void CommitTo(Dictionary<int, string[]> permanentDeletes)
        {
            foreach (var keyPair in Deletes)
            {
                int i;
                if (permanentDeletes.TryGetValue(keyPair.Key, out string[] suggestions))
                {
                    i = suggestions.Length;
                    var newSuggestions = new string[suggestions.Length + keyPair.Value.count];
                    Array.Copy(suggestions, newSuggestions, suggestions.Length);
                    permanentDeletes[keyPair.Key] = suggestions = newSuggestions;
                }
                else
                {
                    i = 0;
                    suggestions = new string[keyPair.Value.count];
                    permanentDeletes.Add(keyPair.Key, suggestions);
                }
                int next = keyPair.Value.first;
                while (next >= 0)
                {
                    var node = Nodes[next];
                    suggestions[i] = node.suggestion;
                    next = node.next;
                    i++;
                }
            }
        }
    }

    private bool DeleteInSuggestionPrefix(string delete, int deleteLen, string suggestion, int suggestionLen)
    {
        if (deleteLen == 0) return true;
        if (prefixLength < suggestionLen) suggestionLen = prefixLength;
        int j = 0;
        for (int i = 0; i < deleteLen; i++)
        {
            char delChar = delete[i];
            while (j < suggestionLen && delChar != suggestion[j]) j++;
            if (j == suggestionLen) return false;
        }
        return true;
    }

    private string[] ParseWords(string text)
    {
        MatchCollection mc = Regex.Matches(text.ToLower(), @"['’\w-[_]]+");

        var matches = new string[mc.Count];
        for (int i = 0; i < matches.Length; i++) matches[i] = mc[i].ToString();
        return matches;
    }

    private HashSet<string> Edits(string word, int editDistance, HashSet<string> deleteWords)
    {
        editDistance++;
        if (word.Length > 1)
        {
            for (int i = 0; i < word.Length; i++)
            {
                string delete = word.Remove(i, 1);
                if (deleteWords.Add(delete))
                {
                    if (editDistance < maxDictionaryEditDistance) Edits(delete, editDistance, deleteWords);
                }
            }
        }
        return deleteWords;
    }

    private HashSet<string> EditsPrefix(string key)
    {
        HashSet<string> hashSet = new HashSet<string>();
        if (key.Length <= maxDictionaryEditDistance) hashSet.Add("");
        if (key.Length > prefixLength) key = key.Substring(0, prefixLength);
        hashSet.Add(key);
        return Edits(key, 0, hashSet);
    }

    private int GetStringHash(string s)
    {
        //return s.GetHashCode();

        int len = s.Length;
        int lenMask = len;
        if (lenMask > 3) lenMask = 3;

        uint hash = 2166136261;
        for (var i = 0; i < len; i++)
        {
            unchecked
            {
                hash ^= s[i];
                hash *= 16777619;
            }
        }

        hash &= this.compactMask;
        hash |= (uint)lenMask;
        return (int)hash;
    }

    private class ChunkArray<T>
    {
        private const int ChunkSize = 4096;
        private const int DivShift = 12;
        public T[][] Values { get; private set; }
        public int Count { get; private set; }
        public ChunkArray(int initialCapacity)
        {
            int chunks = (initialCapacity + ChunkSize - 1) / ChunkSize;
            Values = new T[chunks][];
            for (int i = 0; i < Values.Length; i++) Values[i] = new T[ChunkSize];
        }
        public int Add(T value)
        {
            if (Count == Capacity)
            {
                var newValues = new T[Values.Length + 1][];
                Array.Copy(Values, newValues, Values.Length);
                newValues[Values.Length] = new T[ChunkSize];
                Values = newValues;
            }
            Values[Row(Count)][Col(Count)] = value;
            Count++;
            return Count - 1;
        }
        public void Clear()
        {
            Count = 0;
        }
        public T this[int index]
        {
            get { return Values[Row(index)][Col(index)]; }
            set { Values[Row(index)][Col(index)] = value; }
        }
        private int Row(int index) { return index >> DivShift; }
        private int Col(int index) { return index & (ChunkSize - 1); }
        private int Capacity { get { return Values.Length * ChunkSize; } }
    }

    public List<SuggestItem> LookupCompound(string input)
    {
        return LookupCompound(input, this.maxDictionaryEditDistance);
    }

    public List<SuggestItem> LookupCompound(string input, int editDistanceMax)
    {
        string[] termList1 = ParseWords(input);

        List<SuggestItem> suggestions = new List<SuggestItem>();
        List<SuggestItem> suggestionParts = new List<SuggestItem>();
        var distanceComparer = new EditDistance(this.distanceAlgorithm);

        bool lastCombi = false;
        for (int i = 0; i < termList1.Length; i++)
        {
            suggestions = Lookup(termList1[i], Verbosity.Top, editDistanceMax);

            if ((i > 0) && !lastCombi)
            {
                List<SuggestItem> suggestionsCombi = Lookup(termList1[i - 1] + termList1[i], Verbosity.Top, editDistanceMax);

                if (suggestionsCombi.Count > 0)
                {
                    SuggestItem best1 = suggestionParts[suggestionParts.Count - 1];
                    SuggestItem best2 = new SuggestItem();
                    if (suggestions.Count > 0)
                    {
                        best2 = suggestions[0];
                    }
                    else
                    {
                        best2.term = termList1[i];
                        best2.distance = editDistanceMax + 1;
                        best2.count = (long)((double)10 / Math.Pow((double)10, (double)best2.term.Length)); // 0;
                    }

                    int distance1 = best1.distance + best2.distance;
                    if ((distance1 >= 0) && ((suggestionsCombi[0].distance + 1 < distance1) || ((suggestionsCombi[0].distance + 1 == distance1) && ((double)suggestionsCombi[0].count > (double)best1.count / (double)SymSpell.N * (double)best2.count))))
                    {
                        suggestionsCombi[0].distance++;
                        suggestionParts[suggestionParts.Count - 1] = suggestionsCombi[0];
                        lastCombi = true;
                        goto nextTerm;
                    }
                }
            }
            lastCombi = false;

            if ((suggestions.Count > 0) && ((suggestions[0].distance == 0) || (termList1[i].Length == 1)))
            {
                suggestionParts.Add(suggestions[0]);
            }
            else
            {
                SuggestItem suggestionSplitBest = null;

                if (suggestions.Count > 0) suggestionSplitBest = suggestions[0];

                if (termList1[i].Length > 1)
                {
                    for (int j = 1; j < termList1[i].Length; j++)
                    {
                        string part1 = termList1[i].Substring(0, j);
                        string part2 = termList1[i].Substring(j);
                        SuggestItem suggestionSplit = new SuggestItem();
                        List<SuggestItem> suggestions1 = Lookup(part1, Verbosity.Top, editDistanceMax);
                        if (suggestions1.Count > 0)
                        {
                            List<SuggestItem> suggestions2 = Lookup(part2, Verbosity.Top, editDistanceMax);
                            if (suggestions2.Count > 0)
                            {
                                suggestionSplit.term = suggestions1[0].term + " " + suggestions2[0].term;

                                int distance2 = distanceComparer.Compare(termList1[i], suggestionSplit.term, editDistanceMax);
                                if (distance2 < 0) distance2 = editDistanceMax + 1;

                                if (suggestionSplitBest != null)
                                {
                                    if (distance2 > suggestionSplitBest.distance) continue;
                                    if (distance2 < suggestionSplitBest.distance) suggestionSplitBest = null;
                                }

                                suggestionSplit.distance = distance2;
                                if (bigrams.TryGetValue(suggestionSplit.term, out long bigramCount))
                                {
                                    suggestionSplit.count = bigramCount;

                                    if (suggestions.Count > 0)
                                    {
                                        if ((suggestions1[0].term + suggestions2[0].term == termList1[i]))
                                        {
                                            suggestionSplit.count = Math.Max(suggestionSplit.count, suggestions[0].count + 2);
                                        }
                                        else if ((suggestions1[0].term == suggestions[0].term) || (suggestions2[0].term == suggestions[0].term))
                                        {
                                            suggestionSplit.count = Math.Max(suggestionSplit.count, suggestions[0].count + 1);
                                        }
                                    }
                                    else if ((suggestions1[0].term + suggestions2[0].term == termList1[i]))
                                    {
                                        suggestionSplit.count = Math.Max(suggestionSplit.count, Math.Max(suggestions1[0].count, suggestions2[0].count) + 2);
                                    }

                                }
                                else
                                {
                                    suggestionSplit.count = Math.Min(bigramCountMin, (long)((double)suggestions1[0].count / (double)SymSpell.N * (double)suggestions2[0].count));
                                }

                                if ((suggestionSplitBest == null) || (suggestionSplit.count > suggestionSplitBest.count)) suggestionSplitBest = suggestionSplit;
                            }
                        }
                    }

                    if (suggestionSplitBest != null)
                    {
                        suggestionParts.Add(suggestionSplitBest);
                    }
                    else
                    {
                        SuggestItem si = new SuggestItem();
                        si.term = termList1[i];
                        si.count = (long)((double)10 / Math.Pow((double)10, (double)si.term.Length));
                        si.distance = editDistanceMax + 1;
                        suggestionParts.Add(si);
                    }
                }
                else
                {
                    SuggestItem si = new SuggestItem();
                    si.term = termList1[i];
                    si.count = (long)((double)10 / Math.Pow((double)10, (double)si.term.Length));
                    si.distance = editDistanceMax + 1;
                    suggestionParts.Add(si);
                }
            }
        nextTerm:;
        }

        SuggestItem suggestion = new SuggestItem();

        double count = SymSpell.N;
        System.Text.StringBuilder s = new System.Text.StringBuilder();
        foreach (SuggestItem si in suggestionParts) { s.Append(si.term + " "); count *= (double)si.count / (double)SymSpell.N; }
        suggestion.count = (long)count;

        suggestion.term = s.ToString().TrimEnd();
        suggestion.distance = distanceComparer.Compare(input, suggestion.term, int.MaxValue);

        List<SuggestItem> suggestionsLine = new List<SuggestItem>();
        suggestionsLine.Add(suggestion);
        return suggestionsLine;
    }

    public static long N = 1024908267229L;

    public (string segmentedString, string correctedString, int distanceSum, decimal probabilityLogSum) WordSegmentation(string input)
    {
        return WordSegmentation(input, this.MaxDictionaryEditDistance, this.maxDictionaryWordLength);
    }

    public (string segmentedString, string correctedString, int distanceSum, decimal probabilityLogSum) WordSegmentation(string input, int maxEditDistance)
    {
        return WordSegmentation(input, maxEditDistance, this.maxDictionaryWordLength);
    }

    public (string segmentedString, string correctedString, int distanceSum, decimal probabilityLogSum) WordSegmentation(string input, int maxEditDistance, int maxSegmentationWordLength)
    {
        input = input.Normalize(System.Text.NormalizationForm.FormKC).Replace("\u002D", "");

        int arraySize = Math.Min(maxSegmentationWordLength, input.Length);
        (string segmentedString, string correctedString, int distanceSum, decimal probabilityLogSum)[] compositions = new(string segmentedString, string correctedString, int distanceSum, decimal probabilityLogSum)[arraySize];
        int circularIndex = -1;

        for (int j = 0; j < input.Length; j++)
        {
            int imax = Math.Min(input.Length - j, maxSegmentationWordLength);
            for (int i = 1; i <= imax; i++)
            {
                string part = input.Substring(j, i);
                int separatorLength = 0;
                int topEd = 0;
                decimal topProbabilityLog = 0;
                string topResult = "";

                if (Char.IsWhiteSpace(part[0]))
                {
                    part = part.Substring(1);
                }
                else
                {
                    separatorLength = 1;
                }

                topEd += part.Length;
                part = part.Replace(" ", "");
                topEd -= part.Length;

                List<SymSpell.SuggestItem> results = this.Lookup(part.ToLower(), SymSpell.Verbosity.Top, maxEditDistance);
                if (results.Count > 0)
                {
                    topResult = results[0].term;
                    if ((part.Length>0) && Char.IsUpper(part[0]))
                    {
                        char[] a = topResult.ToCharArray();
                        a[0] = char.ToUpper(topResult[0]);
                        topResult = new string(a);
                    }

                    topEd += results[0].distance;
                    topProbabilityLog = (decimal)Math.Log10((double)results[0].count / (double)N);
                }
                else
                {
                    topResult = part;
                    topEd += part.Length;
                    topProbabilityLog = (decimal)Math.Log10(10.0 / (N * Math.Pow(10.0, part.Length)));
                }

                int destinationIndex = ((i + circularIndex) % arraySize);

                if (j == 0)
                {
                    compositions[destinationIndex] = (part, topResult, topEd, topProbabilityLog);
                }
                else if ((i == maxSegmentationWordLength)
                    || (((compositions[circularIndex].distanceSum + topEd == compositions[destinationIndex].distanceSum) || (compositions[circularIndex].distanceSum + separatorLength + topEd == compositions[destinationIndex].distanceSum)) && (compositions[destinationIndex].probabilityLogSum < compositions[circularIndex].probabilityLogSum + topProbabilityLog))
                    || (compositions[circularIndex].distanceSum + separatorLength + topEd < compositions[destinationIndex].distanceSum))
                {
                    if (((topResult.Length == 1) && char.IsPunctuation(topResult[0])) || ((topResult.Length == 2) && topResult.StartsWith("’")))
                    {
                        compositions[destinationIndex] = (
                        compositions[circularIndex].segmentedString + part,
                        compositions[circularIndex].correctedString + topResult,
                        compositions[circularIndex].distanceSum + topEd,
                        compositions[circularIndex].probabilityLogSum + topProbabilityLog);
                    }
                    else
                    {
                        compositions[destinationIndex] = (
                        compositions[circularIndex].segmentedString + " " + part,
                        compositions[circularIndex].correctedString + " " + topResult,
                        compositions[circularIndex].distanceSum + separatorLength + topEd,
                        compositions[circularIndex].probabilityLogSum + topProbabilityLog);
                    }
                }
            }
            circularIndex++; if (circularIndex == arraySize) circularIndex = 0;
        }
        return compositions[circularIndex];
    }


}
