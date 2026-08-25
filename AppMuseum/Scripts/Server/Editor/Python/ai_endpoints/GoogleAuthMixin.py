"""
GoogleAuthMixin — browser-OAuth Google sign-in for AILLMServer.

Place this next to AlchemySQLFunctions.py (in the ai_endpoints package) and:

    from ai_endpoints.GoogleAuthMixin import GoogleAuthMixin

    class AILLMServer(GoogleAuthMixin):
        def __init__(self, ...):
            ...
            self.db = SQLAlchemy(self.app)
            ...
            self.init_google_auth()   # <-- add this once, at the END of __init__

Install deps (requests you already have):
    pip install google-auth PyJWT

Add to your .env:
    GOOGLE_CLIENT_ID=...          # Web application client id
    GOOGLE_CLIENT_SECRET=...      # Web application client secret
    GOOGLE_REDIRECT_URI=https://your-public-host/auth/google/callback
    APP_SCHEME=com.yourvrexperience.museum
    JWT_SECRET=<long random string>

It defines its own tables (google_user, login_code) so it never touches your
key_value store, and it identifies users by the Google `sub` claim.
"""

import os
import time
import secrets
import urllib.parse

import jwt
import requests
from flask import request, redirect, jsonify, abort
from google.oauth2 import id_token as google_id_token
from google.auth.transport import requests as google_requests

GOOGLE_AUTH_ENDPOINT = "https://accounts.google.com/o/oauth2/v2/auth"
GOOGLE_TOKEN_ENDPOINT = "https://oauth2.googleapis.com/token"


class GoogleAuthMixin:
    SESSION_TTL_SECONDS = 60 * 60 * 24 * 30   # 30-day app session
    ONE_TIME_CODE_TTL = 120                    # seconds to complete the handoff

    # ---- call once from AILLMServer.__init__ (after self.db exists) ----------
    def init_google_auth(self):
        self.google_client_id = os.getenv("GOOGLE_CLIENT_ID", "")
        self.google_client_secret = os.getenv("GOOGLE_CLIENT_SECRET", "")
        self.google_redirect_uri = os.getenv("GOOGLE_REDIRECT_URI", "")
        self.app_scheme = os.getenv("APP_SCHEME", "com.yourvrexperience.museumalcover")
        self.jwt_secret = os.getenv("JWT_SECRET", "")

        db = self.db  # your existing flask_sqlalchemy instance

        class GoogleUser(db.Model):
            __tablename__ = "google_user"
            id = db.Column(db.Integer, primary_key=True)
            sub = db.Column(db.String(255), unique=True, nullable=False, index=True)
            email = db.Column(db.String(320))
            name = db.Column(db.String(320))
            picture = db.Column(db.String(1024))
            created_at = db.Column(db.DateTime, default=db.func.current_timestamp())

        class LoginCode(db.Model):
            __tablename__ = "login_code"
            id = db.Column(db.Integer, primary_key=True)
            code = db.Column(db.String(64), unique=True, nullable=False, index=True)
            sub = db.Column(db.String(255), nullable=False)
            expires_at = db.Column(db.Float, nullable=False)

        self.GoogleUser = GoogleUser
        self.LoginCode = LoginCode
        with self.app.app_context():
            db.create_all()   # creates google_user + login_code (idempotent)

        self.app.add_url_rule('/auth/start', 'auth_start',
                              self.auth_start, methods=['GET'])
        self.app.add_url_rule('/auth/google/callback', 'auth_google_callback',
                              self.auth_google_callback, methods=['GET'])
        self.app.add_url_rule('/auth/exchange', 'auth_exchange',
                              self.auth_exchange, methods=['POST'])
                              
        print (" +++GoogleAuthMixin Initialized++++ ")

    # ---- 1) app opens this in the system browser ----------------------------
    def auth_start(self):
        state = request.args.get("state")
        if not state:
            abort(400, "missing state")
        params = {
            "client_id": self.google_client_id,
            "redirect_uri": self.google_redirect_uri,
            "response_type": "code",
            "scope": "openid email",
            "state": state,
            "prompt": "select_account",
        }
        return redirect(f"{GOOGLE_AUTH_ENDPOINT}?{urllib.parse.urlencode(params)}")

    # ---- 2) Google redirects the browser back here --------------------------
    def auth_google_callback(self):
        if request.args.get("error"):
            err = urllib.parse.quote(request.args["error"])
            return redirect(f"{self.app_scheme}://auth?error={err}")

        code = request.args.get("code")
        state = request.args.get("state")
        if not code or not state:
            abort(400, "missing code/state")

        # Exchange the code server-side (secret never leaves the server).
        resp = requests.post(GOOGLE_TOKEN_ENDPOINT, data={
            "code": code,
            "client_id": self.google_client_id,
            "client_secret": self.google_client_secret,
            "redirect_uri": self.google_redirect_uri,   # must match auth_start exactly
            "grant_type": "authorization_code",
        }, timeout=10)
        resp.raise_for_status()
        tokens = resp.json()

        # Verify signature, iss, aud (== our client id) and expiry.
        claims = google_id_token.verify_oauth2_token(
            tokens["id_token"], google_requests.Request(), self.google_client_id)

        user = self._upsert_google_user(claims)

        otc = secrets.token_urlsafe(32)
        self.db.session.add(self.LoginCode(
            code=otc, sub=user.sub, expires_at=time.time() + self.ONE_TIME_CODE_TTL))
        self.db.session.commit()

        q = urllib.parse.urlencode({"state": state, "code": otc})
        return redirect(f"{self.app_scheme}://auth?{q}")

    # ---- 3) app posts the one-time code, gets its session JWT ---------------
    def auth_exchange(self):
        data = request.get_json(force=True, silent=True) or {}
        otc = data.get("code")
        entry = self.LoginCode.query.filter_by(code=otc).first()
        if entry is None:
            abort(401, "invalid code")

        sub = entry.sub
        expires_at = entry.expires_at
        self.db.session.delete(entry)   # single use, even if expired
        self.db.session.commit()

        if time.time() > expires_at:
            abort(401, "expired code")

        now = int(time.time())
        token = jwt.encode(
            {"sub": sub, "iat": now, "exp": now + self.SESSION_TTL_SECONDS},
            self.jwt_secret, algorithm="HS256")

        user = self.GoogleUser.query.filter_by(sub=sub).first()
        profile = ({"sub": user.sub, "email": user.email,
                    "name": user.name, "picture": user.picture}
                   if user else {"sub": sub})
        return jsonify({"token": token, "user": profile})

    # ---- helpers ------------------------------------------------------------
    def _upsert_google_user(self, claims):
        sub = claims["sub"]  # stable Google account id — key users by THIS, not email
        user = self.GoogleUser.query.filter_by(sub=sub).first()
        if user is None:
            user = self.GoogleUser(
                sub=sub, email=claims.get("email"),
                name=claims.get("name"), picture=claims.get("picture"))
            self.db.session.add(user)
            self.db.session.commit()
        return user

    def require_user(self):
        """
        Call at the top of any protected endpoint. Returns the Google `sub`
        (your user id) or aborts with 401. Use it to replace the
        userid/username/password body fields for app-facing endpoints.
        """
        header = request.headers.get("Authorization", "")
        if not header.startswith("Bearer "):
            abort(401, "missing bearer token")
        try:
            payload = jwt.decode(header[7:], self.jwt_secret, algorithms=["HS256"])
        except jwt.PyJWTError:
            abort(401, "invalid token")
        return payload["sub"]
