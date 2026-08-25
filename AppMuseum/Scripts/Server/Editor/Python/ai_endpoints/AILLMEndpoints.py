from ai_endpoints.AlchemySQLFunctions import AlchemyDBFunctions
from ai_endpoints.GoogleAuthMixin import GoogleAuthMixin

from gradio_client import Client
from enum import Enum
from pydantic import BaseModel, Field
from typing import List
import hashlib
import requests
import base64
from flask import Flask, request, jsonify
from flask_sqlalchemy import SQLAlchemy
from langchain_community.llms import Ollama
import os
import io
import json
import re
import brotli
import time
import binascii
import uuid
# from TTS.api import TTS
from pydub import AudioSegment
from langchain_core.messages import AIMessage, HumanMessage
from langchain_core.output_parsers import JsonOutputParser
from langchain_core.utils.json import parse_json_markdown
from langchain_openai import ChatOpenAI
from openai import OpenAI
from vertexai.preview import tokenization
import tiktoken
from langchain_anthropic import ChatAnthropic
from langchain_mistralai import ChatMistralAI
from langchain_google_genai import ChatGoogleGenerativeAI
from langchain_core.exceptions import OutputParserException
from langchain_core.output_parsers import BaseOutputParser
from langchain_core.prompts import (
    ChatPromptTemplate,
    HumanMessagePromptTemplate,
    MessagesPlaceholder,
    SystemMessagePromptTemplate,
)
from mistral_common.protocol.instruct.messages import (
    UserMessage,
)
from mistral_common.protocol.instruct.request import ChatCompletionRequest
from mistral_common.protocol.instruct.tool_calls import (
    Function,
    Tool,
)
from mistral_common.tokens.tokenizers.mistral import MistralTokenizer
import nltk
from langchain_openai.chat_models.base import BaseChatOpenAI
from ollama import chat
import openai
from flask_cors import CORS
from elevenlabs.client import ElevenLabs
from elevenlabs import Voice, VoiceSettings, save, play

from dotenv import load_dotenv
load_dotenv()

PRICE_URL = "https://raw.githubusercontent.com/BerriAI/litellm/main/model_prices_and_context_window.json"

class ProviderLLM(Enum):
    CHAT_GPT = 1
    ANTHROPIC = 2
    MISTRAL = 3
    GOOGLE = 4
    GROK = 5
    DEEPSEEK = 6
    OPENROUTER = 7
    LOCAL = 8
        
class AILLMServer(GoogleAuthMixin):
    def __init__(self, hostAddress, portNumber, databaseAlchemy, templateQuestion):
        self.host_address = hostAddress
        self.port_number = portNumber
        self.app = Flask(__name__) 
        CORS(self.app, origins=["*"])        
        self.app.config['SQLALCHEMY_DATABASE_URI'] = databaseAlchemy
        self.app.config['SQLALCHEMY_TRACK_MODIFICATIONS'] = False
        self.db = SQLAlchemy(self.app)
        self.is_db_inited = False

        self.template_question = templateQuestion
        self.provider_llm = -1

        self.cached_llm = None
        self.clientOpenAI = None
        self.clientElevenLabs = None
        self.speech_selected = 5
        
        # ++++ REQUIRE USER ++++ 
        self.enable_user_check = False
        self.cost_per_token_input = 0
        self.cost_per_token_output = 0

        self.app.add_url_rule('/store', 'store_value', self.store_value, methods=['POST'])
        self.app.add_url_rule('/init_db', 'init_db', self.init_db, methods=['GET'])
        self.app.add_url_rule('/retrieve', 'retrieve_values', self.retrieve_values, methods=['GET'])
        self.app.add_url_rule('/get_value', 'get_value', self.get_value, methods=['GET'])
        self.app.add_url_rule('/delete', 'delete_value', self.delete_value, methods=['DELETE'])
        self.app.add_url_rule('/clear', 'clear_values', self.clear_values, methods=['DELETE'])
        self.app.add_url_rule('/update', 'update_value', self.update_value, methods=['PUT'])

        self.app.add_url_rule('/', 'index', self.index, methods=['GET'])

        self.app.add_url_rule('/ai/set_provider_llm', 'set_provider_llm', self.set_provider_llm, methods=['POST'])
        self.app.add_url_rule('/ai/get_provider_llm', 'get_provider_llm', self.get_provider_llm, methods=['POST'])

        self.app.add_url_rule('/ai/question', 'question', self.question, methods=['POST'])
        self.app.add_url_rule('/ai/question/history', 'question_history', self.question_history, methods=['POST'])
        self.app.add_url_rule('/ai/question/delete', 'question_delete_history', self.question_delete_history, methods=['POST'])
        self.app.add_url_rule('/ai/question/last_cost', 'get_user_operation_cost', self.get_user_operation_cost, methods=['POST'])

        self.app.add_url_rule('/ai/speech_direct', 'speech_generation', self.speech_generation, methods=['POST'])

        self.app.add_url_rule('/ai/question/delete_last', 'delete_last', self.delete_last, methods=['POST'])
        
        self.app.add_url_rule('/ai/users/login', 'login_user', self.login_user, methods=['GET'])
        self.app.add_url_rule('/ai/users/create', 'create_user', self.create_user, methods=['GET'])

        self.app.add_url_rule('/ai/stop', 'stop', self.stop, methods=['GET'])
        self.app.add_url_rule('/ai/status', 'status', self.status, methods=['GET'])
        
        self.apikey_openai = os.getenv("OPENAI_API_KEY", "")
        self.apikey_mistral = os.getenv("MISTRAL_API_KEY", "")
        self.apikey_google = os.getenv("GOOGLE_API_KEY", "")
        self.apikey_deepseek = os.getenv("DEEPSEEK_API_KEY", "")
        self.apikey_grok = os.getenv("GROK_API_KEY", "")        
        self.apikey_openrouter = os.getenv("OPENROUTER_API_KEY", "")
        self.apikey_elevenlabs = os.getenv("ELEVENLABS_API_KEY", "")
        self.apikey_speechify = os.getenv("SPEECHIFY_API_KEY", "")
        
        self.provider_config_path = os.getenv("PROVIDER_CONFIG_PATH", "provider_config.json")
        self._load_provider_selection()
        self._load_price_map()
        self.init_google_auth()
        print("+++AILLMEndpoints Initialized++++")

    def _load_provider_selection(self):
        self.provider_selected = 3 # ProviderLLM.MISTRAL
        self.model_selected = "open-mistral-nemo-2407"
        self.cost_input_token = 0.0000003
        self.cost_output_token = 0.0000003
        try:
            with open(self.provider_config_path) as f:
                cfg = json.load(f)
            self.provider_selected = cfg.get("provider", "")
            self.model_selected = cfg.get("model", "")
            self.cost_input_token = cfg.get("input_token", "")
            self.cost_output_token = cfg.get("output_token", "")
            self.speech_selected = cfg.get("speech", "5")
        except (FileNotFoundError, json.JSONDecodeError):
            print(" +++INIT++++ No saved selection; waiting for /ai/set_provider_llm")

        if self.provider_selected != "":
            print(f" +++LOAD PROVIDER++++ Provider {ProviderLLM(int(self.provider_selected))}, Model {self.model_selected}, Cost Input {self.cost_input_token}, Cost Output {self.cost_output_token}")
            self.set_llm(self.provider_selected, self.model_selected, self.speech_selected, self.cost_input_token, self.cost_output_token)
        
    def _save_provider_selection(self, provider_value, model_llm, input_token, output_token, speech_provider):
        with open(self.provider_config_path, "w") as f:            
            json.dump({"provider": int(provider_value), "model": model_llm, "input_token": input_token, "output_token": output_token, "speech": speech_provider}, f)
        
    def set_llm(self, llm_provider, model_llm, speech_gen, cost_input_token, cost_output_token):
        # Accept a ProviderLLM, an int, or a numeric string; bail if nothing selected
        if not isinstance(llm_provider, ProviderLLM):
            if llm_provider in ("", None):
                print(" +++LLM++++ No provider selected; skipping LLM init")
                return
            llm_provider = ProviderLLM(int(llm_provider))
        
        # SPEECH & AUDIO GENERATION
        self.clientOpenAI = OpenAI()
        self.clientElevenLabs = ElevenLabs(api_key = self.apikey_elevenlabs)

        # ++++ OPENAI CHATGPT ++++
        if llm_provider == ProviderLLM.CHAT_GPT:
            self.provider_llm = ProviderLLM.CHAT_GPT            
            self.cached_llm = ChatOpenAI(model_name=model_llm)
            self.cost_per_token_input = cost_input_token # GPT4 (input)
            self.cost_per_token_output = cost_output_token # GPT4 (output)
            print (" +++LLM++++ Running OpenAI "+ model_llm)
				
        # ++++ MISTRAL ++++
        if llm_provider == ProviderLLM.MISTRAL:
            self.provider_llm = ProviderLLM.MISTRAL
            self.cached_llm = ChatMistralAI(model=model_llm)
            self.cost_per_token_input = cost_input_token  # mistral-large-latest (input)
            self.cost_per_token_output = cost_output_token # mistral-large-latest (output)
            print (" +++LLM++++ Running Mistral "+model_llm)          

        # ++++ DEEPSEEK ++++
        if llm_provider == ProviderLLM.DEEPSEEK:
            self.provider_llm = ProviderLLM.DEEPSEEK
            self.cached_llm = BaseChatOpenAI(model=model_llm, openai_api_key=self.apikey_deepseek, openai_api_base='https://api.deepseek.com', max_tokens=1024)
            self.cost_per_token_input = cost_input_token
            self.cost_per_token_output = cost_output_token
            print (" +++LLM++++ Running Deepseek "+model_llm)     
            
        # ++++ GOOGLE GEMINI ++++
        if llm_provider == ProviderLLM.GOOGLE:
            self.provider_llm = ProviderLLM.GOOGLE
            self.cached_llm = ChatGoogleGenerativeAI(model=model_llm)
            self.cost_per_token_input = cost_input_token
            self.cost_per_token_output = cost_output_token
            print (" +++LLM++++ Running Google "+model_llm)

        # ++++ GROK ++++
        if llm_provider == ProviderLLM.GROK:
            self.provider_llm = ProviderLLM.GROK
            self.cached_llm = ChatOpenAI(model_name=model_llm, openai_api_key=self.apikey_grok, openai_api_base="https://api.x.ai/v1")
            self.cost_per_token_input = cost_input_token
            self.cost_per_token_output = cost_output_token            
            print(" +++LLM++++ Running Grok (xAI) " + model_llm)
            
        # ++++ OPENROUTER ++++
        if llm_provider == ProviderLLM.OPENROUTER:
            self.provider_llm = ProviderLLM.OPENROUTER
            self.cached_llm = ChatOpenAI(
                                            openai_api_key=self.apikey_openrouter,
                                            openai_api_base='https://openrouter.ai/api/v1',
                                            model_name=model_llm,
                                            model_kwargs={},
                                            default_headers={
                                                "HTTP-Referer": "https://www.aistorybookeditor.com",
                                                "X-Title": "AI Story Book Editor",
                                            },
                                        );
            self.cost_per_token_input = cost_input_token
            self.cost_per_token_output = cost_output_token
            print (" +++LLM++++ Running OpenRouter "+model_llm)

        # ++++ GLOBAL CONFIGURATION (LOCAL LLM) ++++
        if llm_provider == ProviderLLM.LOCAL:
            self.provider_llm = ProviderLLM.LOCAL
            openai.api_base = "http://localhost:11434/v1"
            openai.api_key = "sk-no-key-required"  # Ollama does not require a key
            self.cached_llm = ChatOpenAI(model_name="deepseek-r1:70b", openai_api_base=openai.api_base, openai_api_key=openai.api_key)
            self.cost_per_token_input = 0
            self.cost_per_token_output = 0            
            print ("Running LOCAL OLLAMA mistral-nemo 128K LLM")

        self.cached_llm.temperature = 0.7
                                  
        self.provider_selected = llm_provider.value
        self.model_selected = model_llm
        self.speech_selected = speech_gen
        self._save_provider_selection(llm_provider.value, model_llm, cost_input_token, cost_output_token, self.speech_selected)  # persist only on success
        
    def set_provider_llm(self):
        args = request.args
        prompt = request.json
        userID = int(prompt["userid"])
        username = prompt["username"]
        password = prompt["password"]
        llm_provider = ProviderLLM(int(prompt["provider"]))
        model_llm = prompt["model"]
        speech_gen = int(prompt["speech"])
        cost_input_token = float(prompt["costinput"])
        cost_input_token = float(prompt["costoutput"])        

        print (" +++LLM++++ SET UP PROVIDER TO " + str(llm_provider))
    
        self.set_llm(llm_provider, model_llm, speech_gen, cost_input_token, cost_input_token)
    
        return jsonify({"success": True})
        
    def get_provider_llm(self):
        args = request.args
        prompt = request.json
        userID = int(prompt["userid"])
        username = prompt["username"]
        password = prompt["password"]

        print (" +++LLM++++ GET PROVIDER " + str(self.provider_selected))
    
        return jsonify({"success": True, "provider": self.provider_selected, "model": self.model_selected, "speech": self.speech_selected})        
        
    def init_sql_functions(self, userapp):
        if self.is_db_inited is False:
            self.is_db_inited = True            
            self.sqlFunctions = AlchemyDBFunctions(self.db, userapp)
            with self.app.app_context():
                self.db.create_all()
            print ("+++++++++++++++++++++++++++++AlchemyDBFunctions HAS BEEN INITIALIZED")

    def is_free_llm(self):
        if self.cost_per_token_input > 0 and self.cost_per_token_output > 0:
            return False
        else:
            return True

    def _load_price_map(self):
        try:
            self.price_map = requests.get(PRICE_URL, timeout=10).json()
        except Exception:
            with open("model_prices.json") as f:   # commit a fallback copy to your repo
                self.price_map = json.load(f)

    def calculate_cost_from_usage(self, usage_metadata):
        if not usage_metadata:
            return 0.0
        spec = self.price_map.get(self.model_selected) or {}
        inp = usage_metadata.get("input_tokens", 0)  * spec.get("input_cost_per_token", 0)
        out = usage_metadata.get("output_tokens", 0) * spec.get("output_cost_per_token", 0)
        return inp + out
        
    def store_last_operation_cost(self, name_cost, cost_value):
        if not self.sqlFunctions.exist_value(name_cost):
            self.sqlFunctions.store_new_value(name_cost, str(cost_value * 1000))
        else:
            self.sqlFunctions.update_value(name_cost, str(cost_value * 1000))

    def get_last_operation_cost(self, name_cost):
        if not self.sqlFunctions.exist_value(name_cost):
            return 0
        else:
            cost_string = self.sqlFunctions.get_value_by_name(name_cost)
            return float(cost_string.value)
        
    # -------------------------------------------------------------
    # -------------------------------------------------------------
    # BASE ENDPOINTS
    # -------------------------------------------------------------
    # -------------------------------------------------------------

    def init_db(self):
        username = request.args.get('name')
        self.init_sql_functions(username)
        return jsonify({"message": "DB inited successfully"}), 201

    def store_value(self):
        name = request.json.get('name')
        value = request.json.get('value')
        if not name or not value:
            return jsonify({"error": "Invalid input"}), 400
        
        self.sqlFunctions.store_new_value(name, value)
        return jsonify({"message": "Value stored successfully"}), 201

    def retrieve_values(self):
        result = self.sqlFunctions.get_all_values()
        return jsonify(result), 200

    def get_value(self):
        name = request.args.get('name')
        if not name:
            return jsonify({"error": "Name parameter is required"}), 400
        
        entry = self.sqlFunctions.get_value_by_name(name)
        if entry:
            return jsonify({"name": entry.name, "value": entry.value}), 200
        else:
            return jsonify({"error": "Name not found"}), 404

    def delete_value(self):
        name = request.args.get('name')
        if not name:
            return jsonify({"error": "Name parameter is required"}), 400

        entry = self.sqlFunctions.get_value_by_name(name)
        if entry:
            self.sqlFunctions.delete_value_by_name(name)
            return jsonify({"message": f"Entry with name '{name}' deleted successfully"}), 200
        else:
            return jsonify({"error": "Name not found"}), 404

    def clear_values(self):
        try:
            num_rows_deleted = self.sqlFunctions.delete_all_values()
            return jsonify({"message": f"All entries deleted successfully, {num_rows_deleted} rows affected"}), 200
        except Exception as e:
            self.db.session.rollback()
            return jsonify({"error": str(e)}), 500

    def update_value(self):
        data = request.json
        name = data.get('name')
        new_value = data.get('value')

        if not name or not new_value:
            return jsonify({"error": "Name and new value are required"}), 400

        entry = self.sqlFunctions.get_value_by_name(name)
        if entry:
            self.sqlFunctions.update_value(name, new_value)
            return jsonify({"message": f"Value for name '{name}' updated successfully"}), 200
        else:
            return jsonify({"error": "Name not found"}), 404        

    def extract_json_from_string(self, input_string):
        # Updated regex to handle both objects and arrays
        json_pattern = r'(\{[\s\S]*\}|\[[\s\S]*\])'
        
        match = re.search(json_pattern, input_string)
        
        if match:
            json_string = match.group(0)
            try:
                json_data = json.loads(json_string)
                return json_data
            except json.JSONDecodeError:
                print("Extracted string is not valid JSON.")
                return None
        else:
            print("No JSON data found in the input string.")
            return None
            
    # -------------------------------------------------------------
    # -------------------------------------------------------------
    # AI ENDPOINTS
    # -------------------------------------------------------------
    # -------------------------------------------------------------

    def index(self):
           # self.cached_llm.set_user_id("abc123")
           return self.cached_llm.model + ":CONTEXT[" +  str(self.cached_llm.num_ctx) + "]" # ":GPU["+str(self.cached_llm.num_gpu)  +"]:TEMPERATURE["+ str(self.cached_llm.temperature)+"]"
           # return self.cached_llm.model_name + ":CONTEXT[" +  str(self.cached_llm.num_ctx) + "]" # ":GPU["+str(self.cached_llm.num_gpu)  +"]:TEMPERATURE["+ str(self.cached_llm.temperature)+"]"
           
    # ++ endpoint POST "/ai/question" ++
    # Raw body:
    # {
    #    "userid": 10,
    #    "username": "username",
    #    "password": "passwrod",
    #    "conversationid": "1",
    #    "question": "What can you tell me about the city of London?",
    #    "history": JSON,
    #    "chain": true,
    #    "debug": true
    # }       
    def question(self):
        args = request.args
        prompt = request.json
        userID = int(prompt["userid"])
        username = prompt["username"]
        password = prompt["password"]
        conversationName = prompt["conversationid"]
        question = prompt["question"]
        chain = bool(prompt["chain"])

        debug = args.get("debug", default=False, type=bool)
        if debug:
            print("AI question received...")
            print("AI question is {}".format(question))

        self.init_sql_functions(username)
        if self.enable_user_check and not self.sqlFunctions.login_user_id(
            userID, username, password, self.port_number
        ):
            return "Error: No matching user and password"

        response = None

        if chain:
            # History now comes from the client instead of SQL
            history = prompt.get("history", {})
            if isinstance(history, str):              # Unity sent it as a JSON string
                history = json.loads(history) if history else {}

            history_messages = []
            for msg in history.get("Messages") or []:
                if msg["Mode"] == 0:                  # 0 = Human, 1 = AI
                    message_human = msg["Text"]
                    history_messages.append(HumanMessage(content=message_human))
                else:
                    message_ai = msg["Text"]
                    history_messages.append(AIMessage(content=message_ai))

            prompt_template = ChatPromptTemplate.from_messages([
                ("system", self.template_question),
                MessagesPlaceholder(variable_name="history"),
                ("human", "{input}"),
            ])
            conversation = prompt_template | self.cached_llm

            ai_message = conversation.invoke({"history": history_messages, "input": question})
            response = ai_message.content if isinstance(ai_message, AIMessage) else str(ai_message)

            usage = getattr(ai_message, "usage_metadata", None)
            self.store_last_operation_cost(username + "_cost", self.calculate_cost_from_usage(usage))            
        else:
            ai_message = self.cached_llm.invoke(question)
            response = ai_message.content if isinstance(ai_message, AIMessage) else str(ai_message)

            usage = getattr(ai_message, "usage_metadata", None)
            self.store_last_operation_cost(username + "_cost", self.calculate_cost_from_usage(usage))            

        print(response)
        if debug:
            print("AI response received...")
        return response
        
    def question_history(self):
            args = request.args
            prompt = request.json
            userID = int(prompt["userid"])
            username = prompt["username"]
            password = prompt["password"]
            conversationName = prompt["conversationid"]

            if args.get("debug", default=False, type=bool):
                print("AI history get received...")

            self.init_sql_functions(username)

            if self.enable_user_check and not self.sqlFunctions.login_user_id(userID, username, password, self.port_number):
                return "Error: No matching user and password"            

            output = ""
            if not self.sqlFunctions.exist_value(conversationName):
                return output
            else:
                historyJSON = self.sqlFunctions.get_history_by_name(conversationName)
            
                if len(historyJSON) > 1:
                    json_messages = []
                    messages = self.sqlFunctions.get_list_messages(historyJSON)
                    for user_msg, ai_msg in messages:
                        json_object_user = { "Mode": 1, "Text": user_msg }
                        json_messages.append(json_object_user)
                        json_object_ai = { "Mode": 0, "Text": ai_msg }
                        json_messages.append(json_object_ai)

                    output = json.dumps(json_messages)  
        
                print (output)
                
                if args.get("debug", default=False, type=bool):
                        print("AI history response produced...")
                        
                return output

    def question_delete_history(self):
            args = request.args
            prompt = request.json
            userID = int(prompt["userid"])
            username = prompt["username"]
            password = prompt["password"]
            conversationName = prompt["conversationid"]

            if args.get("debug", default=False, type=bool):
                print("AI history delete received...")

            self.init_sql_functions(username)

            if self.enable_user_check and not self.sqlFunctions.login_user_id(userID, username, password, self.port_number):
                return "Error: No matching user and password"

            if not self.sqlFunctions.exist_value(conversationName):
                return "Error"
            else:
                self.sqlFunctions.delete_value_by_name(conversationName)
                
                if args.get("debug", default=False, type=bool):
                        print("AI history response produced...")
                        
                return "true"
    
    def get_user_operation_cost(self):
            args = request.args
            prompt = request.json
            userID = int(prompt["userid"])
            username = prompt["username"]
            password = prompt["password"]

            if args.get("debug", default=False, type=bool):
                print("AI get last operation cost received...")

            self.init_sql_functions(username)

            if self.enable_user_check and not self.sqlFunctions.login_user_id(userID, username, password, self.port_number):
                return "Error: No matching user and password"            
            
            cost_operation = self.get_last_operation_cost(username + "_cost")

            if args.get("debug", default=False, type=bool):
                    print("AI get operation cost = " + str(cost_operation))

            return jsonify({"cost": cost_operation, "response": str(cost_operation)})

    def delete_last(self):
            args = request.args
            prompt = request.json
            userID = int(prompt["userid"])
            username = prompt["username"]
            password = prompt["password"]
            conversationName = prompt["conversationid"]

            if args.get("debug", default=False, type=bool):
                print("AI delete last question received...")

            self.init_sql_functions(username)

            if self.enable_user_check and not self.sqlFunctions.login_user_id(userID, username, password, self.port_number):
                return "Error: No matching user and password"            

            if self.sqlFunctions.delete_last_committed_value(conversationName):
                return "true"
            else:
                return "false"
    
     # ++ endpoint POST "/ai/speech" ++
    # Raw body:
    # {
    #    "userid": -1,
    #    "username": "username",
    #    "password": "password",
    #    "voice": "HalleB1.wav",
    #    "speech": "Hello world! How are you today?",
    #    "language": "en",
    #    "emotion": "",
    #    "speed": 1
    # }
    def speech_generation(self) -> bytes:
            args = request.args
            prompt = request.json
            userID = int(prompt["userid"])            
            username = prompt["username"]
            password = prompt["password"]
            voice = prompt["voice"]
            speech = prompt["speech"]
            language = prompt["language"]
            emotion = prompt["emotion"]

            self.init_sql_functions(username)

            if self.enable_user_check and not self.sqlFunctions.login_user_id(userID, username, password, self.port_number):
                return "Error: No matching user and password"            

            print ("SPEECH GENERATION::PROVIDER["+str(self.speech_selected)+"]::TEXT="+speech)

            try:
                # //////// ELEVENLABS PROVIDER ////////
                if self.speech_selected == 1: 
                    finalVoiceID = voice                
                    if finalVoiceID is not None:
                        temp_mp3_eleven_file = f"outputEleven_{uuid.uuid4().hex}.mp3"
                        stability = 0.5
                        similarity_boost = 0.75
                        style = 0
                        speed = 1.2
                        responseElevenLabs = self.clientElevenLabs.text_to_speech.convert(
                            text=speech,
                            voice_id=finalVoiceID,
                            voice_settings=VoiceSettings(stability=stability, similarity_boost=similarity_boost, style=style, use_speaker_boost=True, speed=speed),
                            model_id="eleven_multilingual_v2"
                        )
                        save(responseElevenLabs, temp_mp3_eleven_file)
                        dataaudio = AudioSegment.from_mp3(temp_mp3_eleven_file).export(format="ogg")
                        os.remove(temp_mp3_eleven_file)
                        return dataaudio

                # //////// OPENAI PROVIDER ////////
                elif self.speech_selected == 2:
                    temp_mp3_openai_file = f"outputOpenAI_{uuid.uuid4().hex}.mp3"
                    responseOpenAI = self.clientOpenAI.audio.speech.create(
                                                                model="tts-1",
                                                                voice=voice,
                                                                input=speech
                                                                )
                    responseOpenAI.stream_to_file(temp_mp3_openai_file)
                    dataaudio = AudioSegment.from_mp3(temp_mp3_openai_file).export(format="ogg")
                    os.remove(temp_mp3_openai_file)
                    return dataaudio   
                            
                # //////// SPEECHIFY PROVIDER ////////
                elif self.speech_selected == 5:
                    url = "https://api.sws.speechify.com/v1/audio/speech"

                    finalID = voice

                    final_emotion = "relaxed"

                    text_to_speech = "<speak>" + speech + "</speak>"
                    if (len(final_emotion) > 0):
                        text_to_speech = "<speak>"
                        text_to_speech += "<speechify:style emotion=\""+final_emotion+"\">"
                        text_to_speech += speech
                        text_to_speech += "</speechify:style>"
                        text_to_speech += "</speak>"

                    if finalID is not None:
                        payload = {
                            "voice_id": finalID,
                            "audio_format": "mp3",
                            "input": text_to_speech,
                            "language": language
                        }
                        headers = {
                            "Authorization": f"Bearer {self.apikey_speechify}",
                            "Content-Type": "application/json"
                        }
                        response = requests.post(url, json=payload, headers=headers)

                        print(response.status_code)
                        if response.status_code == 200:
                            response_data = response.json()

                            audio_base64 = response_data.get("audio_data")
                            if audio_base64:
                                audio_data = base64.b64decode(audio_base64)
                            
                                tmp_speechify_audio_mp3 = f"speechify-audio-output_{uuid.uuid4().hex}.mp3"
                                with open(tmp_speechify_audio_mp3, "wb") as audio_file:
                                    audio_file.write(audio_data)
                                    
                                # Export to OGG with the desired sample rate and bitrate
                                # - `codec="libvorbis"` ensures we’re using Vorbis inside the OGG container
                                # - `bitrate="66k"` aims for 66 kb/s
                                # - `parameters=["-ar", "44100"]` forces a 44.1 kHz sample rate
                                dataaudio = AudioSegment.from_mp3(tmp_speechify_audio_mp3).export(
                                    format="ogg",
                                    codec="libvorbis",
                                    bitrate="66k",
                                    parameters=["-ar", "44100"]
                                )
                                
                                os.remove(tmp_speechify_audio_mp3)
                            
                                return dataaudio           
                        else:
                            print(f"Error: {response.status_code}")
                            print(response.text)
                    else:
                        print(f"Error: No final ID found")

            except Exception as ex:
                return "Error: Exception " + str(ex)

    # ++++++++++++++++++++++
    # ++++++++++++++++++++++
    # OTHERS
    # ++++++++++++++++++++++
    # ++++++++++++++++++++++
           
    def login_user(self):
            args = request.args
            username = args.get("user", default="", type=str)
            password = args.get("password", default="", type=str)

            if args.get("debug", default=False, type=bool):
                    print("Login requested. User("+username+"), Psw("+password+")")

            self.init_sql_functions(username)

            id_user = self.sqlFunctions.validate_password(username, password)
            
            if id_user != -1:
                    return jsonify({"success": True, "user_id": id_user})
            else:
                    return jsonify({"success": False, "user_id": -1})        

    def create_user(self):
            args = request.args
            username = args.get("user", default="", type=str)
            password = args.get("password", default="", type=str)

            if args.get("debug", default=False, type=bool):
                    print("Create user requested. User("+username+"), Psw("+password+")")

            self.init_sql_functions(username)

            id_user = self.sqlFunctions.validate_password(username, password)
            
            if id_user == -1:
                self.sqlFunctions.store_new_value(username, password)
            
            return jsonify({"success": True})
    
    def stop(self):
            return jsonify(status="ok")

    def status(self):
            return jsonify(status="ok")
        
    def start_webserver(self):
            self.app.run(host=self.host_address, port=self.port_number, threaded=False)
        

