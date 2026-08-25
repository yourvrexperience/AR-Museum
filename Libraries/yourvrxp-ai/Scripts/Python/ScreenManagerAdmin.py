import subprocess
import threading
import time
from flask import Flask, request, jsonify
import hashlib
import base64
import time
from flask_cors import CORS

class ScreenManager:
    def __init__(self):
        self.sessions = {}  # Store session details
        self.TIME_WINDOW = 300  # 5 minutes: Time window for verification

    def generate_timestamp(self):
        return str(int(time.time()))

    def generate_combined_salt(self, salt, timestamp):
        # Combine salt and timestamp
        salt_with_timestamp = f"{salt}{timestamp}"
        # Hash the combination
        hashed = hashlib.sha256(salt_with_timestamp.encode('utf-8')).digest()
        return base64.b64encode(hashed).decode('utf-8')

    def hash_with_salt(self, input_str, combined_salt):
        # Hash the input with the combined salt
        combined = (input_str + combined_salt).encode('utf-8')
        hashed = hashlib.sha256(combined).digest()
        return base64.b64encode(hashed).decode('utf-8')

    def verify_user_hash(self, user_id, provided_hash, original_salt, timestamp):
        # Validate the timestamp
        current_time = int(time.time())
        if abs(current_time - int(timestamp)) > self.TIME_WINDOW:
            return False  # Timestamp expired

        # Recompute combined salt and hash
        recomputed_salt = self.generate_combined_salt(original_salt, timestamp)
        computed_hash = self.hash_with_salt(user_id, recomputed_salt)
        return computed_hash == provided_hash    
        
    def session_exists(self, session_name):
        """Check if a session with the given name exists."""
        return session_name in self.sessions

    def get_max_id_session(self):
        """Get the max identification."""
        return max(
            (session_data["identification"] for session_data in self.sessions.values()),
            default=0,
        )

    def get_session_details(self, session_name):
        """Get the session details (name and port number)."""
        if session_name not in self.sessions:
            return {"error": f"Session '{session_name}' does not exist."}, 404

        port_number = self.sessions[session_name]["port_number"]
        return {"session_name": session_name, "port_number": port_number}
            
    def create_session(self, session_name, script_path, timeout_seconds):
        if session_name in self.sessions:
            return {"error": f"Session '{session_name}' already exists."}

        max_id = self.get_max_id_session()
        final_port = 5001 + max_id
        
        # Start the screen session
        subprocess.run([
            "screen", "-dmS", session_name, 
            "bash", "-c", f"python {script_path} --port {final_port}"
        ])
        
        # Start a thread to monitor session timeout
        session_thread = threading.Thread(
            target=self._monitor_session, 
            args=(session_name,),
            daemon=True
        )
        session_thread.start()

        # Store session details
        self.sessions[session_name] = {
            "identification": max_id,
            "script_path": script_path,
            "timeout_seconds": timeout_seconds,
            "start_time": time.time(),
            "thread": session_thread,
            "port_number": final_port,
        }

        print ("==CREATED SESSION["+session_name+"]::SCRIPT["+script_path+"]::PORT["+str(final_port)+"]")

        return {"session_name": session_name, "port_number": final_port}

    def _monitor_session(self, session_name):
        """Monitor session timeout and terminate if time runs out."""
        while True:
            time.sleep(30)
            
            session_data = self.sessions.get(session_name)
            if not session_data:
                print ("==TIMEOUT NO SESSION FOUND FOR["+session_name+"]")
                break

            elapsed_time = time.time() - session_data["start_time"]
            # print ("==elapsed_time["+str(elapsed_time)+"]")
            if elapsed_time >= session_data["timeout_seconds"]:
                print ("==TIMEOUT SESSION["+session_name+"]")
                self.destroy_session(session_name)
                break            

    def destroy_session(self, session_name):
        """Terminate a screen session."""
        if session_name not in self.sessions:
            return {"error": f"Session '{session_name}' does not exist."}

        # Terminate the screen session
        subprocess.run(["screen", "-S", session_name, "-X", "quit"])

        # Remove from the session manager
        del self.sessions[session_name]

        print ("==DESTROYED ++step final++ SESSION["+session_name+"]")

        return {"session_name": session_name}

    def refesh_time_to_session(self, session_name, additional_seconds):
        """Increase the timeout for a session."""
        if session_name not in self.sessions:
            return {"error": f"Session '{session_name}' does not exist."}

        self.sessions[session_name]["start_time"] = time.time()
        return {
            "session_name": session_name,
            "start_time": self.sessions[session_name]["start_time"],
        }

    def list_sessions(self):
        """List all active sessions."""
        return {
            session_name: {
                "script_path": details["script_path"],
                "timeout_seconds": details["timeout_seconds"],
                "elapsed_time": time.time() - details["start_time"],
            }
            for session_name, details in self.sessions.items()
        }

    def get_port_number_session(self, session_name):
        """Get the port number of a session."""
        if session_name not in self.sessions:
            return -1
        return self.sessions[session_name]["port_number"]

# Flask app to manage sessions
app = Flask(__name__)
screen_manager = ScreenManager()

CORS(app, origins=["*"])

@app.route("/create_session", methods=["POST"])
def create_session():
    """Endpoint to create a new screen session."""
    data = request.json
    session_name = data.get("session_name")
    script_path = data.get("script_path")
    timeout_seconds = data.get("timeout_seconds", 60)
    script_path = data.get("script_path")
    salt = data.get("salt")
    user_hash = data.get("user_hash")
    timestamp = data.get("timestamp")

    print ("++++CREATING NEW SESSION["+session_name+"]::SCRIPT["+script_path+"]::TIME["+str(timeout_seconds)+"]")

    # Verify the hash    
    if screen_manager.verify_user_hash(session_name, user_hash, salt, timestamp) is False:
        print ("++++CREATING NEW SESSION::VALIDATION FAILED")
        return jsonify({"error": "User verification invalid"}), 400

    if not session_name or not script_path:
        return jsonify({"error": "Missing session_name or script_path"}), 400

    if screen_manager.session_exists(session_name):
        print ("++++CREATING AND DESTROYING PREVIOUS SESSION")
        screen_manager.destroy_session(session_name)

    return jsonify(screen_manager.create_session(session_name, script_path, timeout_seconds))
	
@app.route("/refresh_time_session", methods=["POST"])
def refresh_time_session():
    """Endpoint to add time to an existing session."""
    data = request.json
    session_name = data.get("session_name")
    additional_seconds = data.get("additional_seconds")
    salt = data.get("salt")
    user_hash = data.get("user_hash")
    timestamp = data.get("timestamp")
    
    print ("++++ADDING TIME SESSION["+session_name+"]")

    # Verify the hash    
    if screen_manager.verify_user_hash(session_name, user_hash, salt, timestamp) is False:
        print ("++++ADDING TIME SESSION::VALIDATION FAILED")
        return jsonify({"error": "User verification invalid"}), 400

    if not session_name or additional_seconds is None:
        return jsonify({"error": "Missing session_name or additional_seconds"}), 400

    if not isinstance(additional_seconds, (int, float)) or additional_seconds <= 0:
        return jsonify({"error": "additional_seconds must be a positive number"}), 400

    result = screen_manager.refesh_time_to_session(session_name, additional_seconds)
    return jsonify(result)	

@app.route("/destroy_session", methods=["POST"])
def destroy_session():
    """Endpoint to destroy a screen session."""
    data = request.json
    session_name = data.get("session_name")
    salt = data.get("salt")
    user_hash = data.get("user_hash")
    timestamp = data.get("timestamp")
    
    print ("++++DESTROYING SESSION["+session_name+"]")
    
    # Verify the hash    
    if screen_manager.verify_user_hash(session_name, user_hash, salt, timestamp) is False:
        print ("++++DESTROYING SESSION::VALIDATION FAILED")
        return jsonify({"error": "User verification invalid"}), 400
    
    result = screen_manager.destroy_session(session_name)
    return jsonify(result)

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000)