# db_functions.py
from flask_sqlalchemy import SQLAlchemy
from langchain.schema.messages import HumanMessage, AIMessage
import json
import hashlib
import base64

class KeyValueMixin:
    @classmethod
    def init_model(cls, db):
        class KeyValue(db.Model):
            __tablename__ = 'key_value'
            id = db.Column(db.Integer, primary_key=True)
            name = db.Column(db.String(200), nullable=False)
            value = db.Column(db.String(1048576), nullable=False)

        class KeyValueHistory(db.Model):
            __tablename__ = 'key_value_history'
            id = db.Column(db.Integer, primary_key=True)
            name = db.Column(db.String(200), nullable=False)
            value = db.Column(db.String(1048576), nullable=False)
            timestamp = db.Column(db.DateTime, default=db.func.current_timestamp())
        
        cls.KeyValue = KeyValue
        cls.KeyValueHistory = KeyValueHistory     

class AlchemyDBFunctions(KeyValueMixin):
    def __init__(self, db: SQLAlchemy, userapp):
        self.db = db
        self.init_model(db)  # Initialize the KeyValue model
        self.userapp = userapp

    def store_new_value(self, name, value):
        new_entry = self.KeyValue(name=name + "_" + self.userapp, value=value)
        self.db.session.add(new_entry)
        self.db.session.commit()
        
    def get_all_values(self):
        values = self.KeyValue.query.all()
        result = [{"name": v.name, "value": v.value} for v in values]
        return result
        
    def get_value_by_name(self, name):
        entry = self.KeyValue.query.filter_by(name=name + "_" + self.userapp).first()
        return entry
        
    def exist_value(self, name):
        entry = self.get_value_by_name(name)
        return entry is not None

    def update_value(self, name, new_value):
        full_name = name + "_" + self.userapp
        entry = self.KeyValue.query.filter_by(name=full_name).first()
        if entry:
            # Delete any existing history for this key (keep only one history)
            existing_history = self.KeyValueHistory.query.filter_by(name=full_name).first()
            if existing_history:
                self.db.session.delete(existing_history)

            # Store current value in history before updating
            history_entry = self.KeyValueHistory(name=full_name, value=entry.value)
            self.db.session.add(history_entry)
            
            # Update the current value
            entry.value = new_value
            self.db.session.commit()
            return True
        else:
            return False

    def delete_value_by_name(self, name):
        entry = self.get_value_by_name(name)
        if entry:
            self.db.session.delete(entry)
            self.db.session.commit()
            return True
        else:
            return False

    def delete_all_values(self):
        num_rows_deleted = self.db.session.query(self.KeyValue).delete()
        self.db.session.commit()
        return num_rows_deleted

    def delete_last_committed_value(self, name):
        """Delete the current value and restore the last committed value from history."""
        full_name = name + "_" + self.userapp
        entry = self.get_value_by_name(name)

        if entry:
            # Find the most recent history for this key
            last_history_entry = self.KeyValueHistory.query.filter_by(name=full_name).order_by(self.KeyValueHistory.timestamp.desc()).first()

            if last_history_entry:
                # Restore the value from history
                entry.value = last_history_entry.value

                # Commit the restoration
                self.db.session.commit()

                # Optionally, delete the history entry (so we don't store too many old values)
                self.db.session.delete(last_history_entry)
                self.db.session.commit()

                return True
            else:
                return False  # No history to restore from
        else:
            return False
            
    def get_history_by_name(self, name):
        entry = self.get_value_by_name(name)
        return entry.value if entry else ""

    def get_list_messages(self, json_string):
        json_data = json.loads(json_string)
        messages = []
        for item in json_data.get('items', []):
            user_message = item.get('user')
            ai_message = item.get('ai')
            messages.append((user_message, ai_message))
        return messages

    def add_new_message(self, json_string, user_message, ai_message):
        json_data = {}
        if len(json_string) > 1:
            json_data = json.loads(json_string)
        new_item = {
            "user": user_message,
            "ai": ai_message
        }
        if 'items' not in json_data:
            json_data['items'] = []
        json_data['items'].append(new_item)
        json_string = json.dumps(json_data, indent=4)
        return json_string
        
    def get_ai_message_content(self, output):
        history = output.get('history', [])
        if len(history) > 0:
            message = history[-1]
            return message.content if isinstance(message, AIMessage) else None
        return None

    def get_unique_id(self, username, port_number, length = 9):
        if not isinstance(username, str):
            username = str(username)    
            
        username += "_" + str(port_number)
            
        if len(username) < length:
            length = len(username)
            
        hash_object = hashlib.sha256(username.encode())
        hash_bytes = hash_object.digest()
        base64_hash = base64.urlsafe_b64encode(hash_bytes).decode('utf-8')
        numeric_part = ''.join(filter(str.isdigit, base64_hash))
        unique_id = int(numeric_part[:length])
        return unique_id
            
    def login_user_id(self, userID, username, password, port_number):
        return self.get_unique_id(username, port_number) == int(password)
