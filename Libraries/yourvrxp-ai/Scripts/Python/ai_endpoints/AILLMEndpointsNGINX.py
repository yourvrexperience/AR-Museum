from ai_endpoints.AlchemySQLFunctions import AlchemyDBFunctions

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
import torch
import brotli
import time
import binascii
# from TTS.api import TTS
from pydub import AudioSegment
from langchain.schema.messages import HumanMessage, AIMessage
from langchain.chains import ConversationChain
from langchain_core.output_parsers import JsonOutputParser
from langchain_core.prompts import PromptTemplate
from langchain_core.utils.json import parse_json_markdown
from langchain_openai import ChatOpenAI
from openai import OpenAI
from vertexai.preview import tokenization
import tiktoken
from langchain_anthropic import ChatAnthropic
from langchain_mistralai import ChatMistralAI
from langchain_google_genai import ChatGoogleGenerativeAI
from langchain.memory import ConversationBufferMemory 
from langchain_core.prompts.prompt import PromptTemplate
from langchain.memory import ConversationSummaryMemory
from langchain_core.exceptions import OutputParserException
from langchain_core.output_parsers import BaseOutputParser
from langchain.prompts import (
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
from elevenlabs.client import ElevenLabs
from elevenlabs import Voice, VoiceSettings, save, play
from forcealign import ForceAlign
import nltk
from langchain_openai.chat_models.base import BaseChatOpenAI
import traceback
from requests.auth import HTTPBasicAuth
from cartesia import Cartesia
# import torchaudio
# from audiocraft.models import AudioGen
# from audiocraft.data.audio import audio_write

class ProviderLLM(Enum):
    CHAT_GPT = 1
    ANTHROPIC = 2
    MISTRAL = 3
    GOOGLE = 4
    DEEPSEEK = 5
    OPENROUTER = 6
    LOCAL = 7
        
class AILLMServer:
    def __init__(self, hostAddress, portNumber, databaseAlchemy, voicesLanguage, urlSpeechGeneration, urlImageGeneration, urlFluxImageGeneration, templateQuestion,  parserFormatImage, promptFormatSoundFX, parserFormatSoundFX, promptFormatMusicLoop, parserFormatMusicLoop, promptFormatTranslateToken, parserFormatTranslateToken, promptParagraphElevenLabsVoiceSettings, parserParagraphElevenLabsVoiceSettings, templateTranslation):
        self.host_address = hostAddress
        self.port_number = portNumber
        self.app = Flask(__name__)        
        self.app.config['SQLALCHEMY_DATABASE_URI'] = databaseAlchemy
        self.app.config['SQLALCHEMY_TRACK_MODIFICATIONS'] = False
        # app.config['wav_voices'] = '/home/esteban/Workspace/TTS/wav_voices'  # Set this to your desired directory
        self.app.config['wav_voices'] = voicesLanguage
        self.db = SQLAlchemy(self.app)
        self.is_db_inited = False

        self.template_question = templateQuestion
        self.provider_llm = -1

        self.cached_llm = None
        self.clientOpenAI = None
        self.clientElevenLabs = None
        
        # ++++ REQUIRE USER ++++ 
        # -If "True" you will need an stored user+password in the database in order to access the service
        # -Endpoint to create user: "/ai/users/create"
        self.enable_user_check = True

        # self.url_speech_generation = urlSpeechGeneration
        self.url_speech_generation = "https://gfhdfghfdgbfs.ngrok-free.app/"
        self.username_xtts = "adfhfcvbxcc"
        self.password_xtts = "tyuityuituyi"

        # self.url_flux_image_generation = urlFluxImageGeneration
        self.url_flux_image_generation = "https://iukyjyhgtfdeggh.ngrok-free.app"
        self.username_flux = "weqweawe"
        self.password_flux = "ghhdgbsdfge"
        
        self.cost_per_token_input = 0
        self.cost_per_token_output = 0

        self.url_image_generation = urlImageGeneration

        self.promptParagraphElevenLabsVoiceSettings = promptParagraphElevenLabsVoiceSettings

        self.parserFormatImage = parserFormatImage
        self.parserFormatSoundFX = parserFormatSoundFX
        self.parserFormatMusicLoop = parserFormatMusicLoop
        self.parserFormatTranslateToken = parserFormatTranslateToken
        self.parserParagraphElevenLabsVoiceSettings = parserParagraphElevenLabsVoiceSettings
        
        self.templateTranslation = templateTranslation

        self.app.add_url_rule('/store', 'store_value', self.store_value, methods=['POST'])
        self.app.add_url_rule('/init_db', 'init_db', self.init_db, methods=['GET'])
        self.app.add_url_rule('/retrieve', 'retrieve_values', self.retrieve_values, methods=['GET'])
        self.app.add_url_rule('/get_value', 'get_value', self.get_value, methods=['GET'])
        self.app.add_url_rule('/delete', 'delete_value', self.delete_value, methods=['DELETE'])
        self.app.add_url_rule('/clear', 'clear_values', self.clear_values, methods=['DELETE'])
        self.app.add_url_rule('/update', 'update_value', self.update_value, methods=['PUT'])

        self.app.add_url_rule('/', 'index', self.index, methods=['GET'])

        self.app.add_url_rule('/ai/init_api_keys', 'init_api_keys', self.init_api_keys, methods=['POST'])
        self.app.add_url_rule('/ai/set_provider_llm', 'set_provider_llm', self.set_provider_llm, methods=['POST'])

        self.app.add_url_rule('/ai/question', 'question', self.question, methods=['POST'])
        self.app.add_url_rule('/ai/question/history', 'question_history', self.question_history, methods=['POST'])
        self.app.add_url_rule('/ai/question/delete', 'question_delete_history', self.question_delete_history, methods=['POST'])
        self.app.add_url_rule('/ai/question/voice_settings_elevenlabs', 'voice_settings_elevenlabs', self.voice_settings_elevenlabs, methods=['POST'])
        self.app.add_url_rule('/ai/question/last_cost', 'get_user_operation_cost', self.get_user_operation_cost, methods=['POST'])
        self.app.add_url_rule('/ai/translation_text', 'translation_text', self.translation_text, methods=['POST'])        

        self.app.add_url_rule('/ai/question/delete_last', 'delete_last', self.delete_last, methods=['POST'])
        
        self.app.add_url_rule('/ai/users/login', 'login_user', self.login_user, methods=['GET'])
        self.app.add_url_rule('/ai/users/create', 'create_user', self.create_user, methods=['GET'])
        self.app.add_url_rule('/ai/conversations/new', 'new_conversation', self.new_conversation, methods=['GET'])
        self.app.add_url_rule('/ai/conversations/get', 'get_conversation', self.get_conversation, methods=['GET'])
        self.app.add_url_rule('/ai/conversations/delete', 'delete_conversation', self.delete_conversation, methods=['GET'])
        self.app.add_url_rule('/ai/conversations/delete_all', 'delete_all_conversations', self.delete_all_conversations, methods=['GET'])

        self.app.add_url_rule('/ai/image', 'image_generation', self.image_generation, methods=['POST'])
        self.app.add_url_rule('/ai/image/derivation', 'image_derivation', self.image_derivation, methods=['POST'])
        self.app.add_url_rule('/ai/speech', 'speech_generation', self.speech_generation, methods=['POST'])
        self.app.add_url_rule('/ai/speech/voice', 'upload_speech_voice', self.upload_speech_voice, methods=['POST'])
        self.app.add_url_rule('/ai/audio', 'audio_generation', self.audio_generation, methods=['POST'])
        self.app.add_url_rule('/ai/music', 'music_generation', self.music_generation, methods=['POST'])        
        self.app.add_url_rule('/ai/align_audio', 'align_text_audio', self.align_text_audio, methods=['POST'])
        self.app.add_url_rule('/ai/list_voices', 'list_voices', self.list_voices, methods=['POST'])
        
        self.app.add_url_rule('/ai/format/image', 'format_image_generation', self.format_image_generation, methods=['POST'])
        self.app.add_url_rule('/ai/format/soundfx', 'format_soundfx_generation', self.format_soundfx_generation, methods=['POST'])
        self.app.add_url_rule('/ai/format/musicloop', 'format_musicloop_generation', self.format_musicloop_generation, methods=['POST'])

        self.app.add_url_rule('/ai/stop', 'stop', self.stop, methods=['GET'])
        self.app.add_url_rule('/ai/status', 'status', self.status, methods=['GET'])
        
        nltk.download('averaged_perceptron_tagger_eng')

    def init_api_keys(self):
        args = request.args
        prompt = request.json
        userID = int(prompt["userid"])
        username = prompt["username"]
        password = prompt["password"]
        apikey_openai = prompt["apikey_openai"]
        apikey_mistral = prompt["apikey_mistral"]
        apikey_google = prompt["apikey_google"]
        apikey_deepseek = prompt["apikey_deepseek"]
        apikey_openrouter = prompt["apikey_openrouter"]
        apikey_stability = prompt["apikey_stability"] 
        apikey_sceneario = prompt["apikey_sceneario"]
        apikey_elevenlabs = prompt["apikey_elevenlabs"]
        apikey_lmnt = prompt["apikey_lmnt"]
        apikey_cartesia = prompt["apikey_cartesia"]
        apikey_speechify = prompt["apikey_speechify"]
        apikey_playht = prompt["apikey_playht"]
        speech_server = prompt["speech_server"]
        image_server = prompt["image_server"]
        audio_server = prompt["audio_server"]

        self.url_speech_generation = speech_server
        self.url_flux_image_generation = image_server
        self.url_audio_generation = audio_server

        # OPEN AI KEY
        os.environ["OPENAI_API_KEY"] = apikey_openai

        # DEEPSEEK KEY
        self.apikey_deepseek = apikey_deepseek
        os.environ["DEEPSEEK_API_KEY"] = apikey_deepseek

        # STABILITY KEY
        self.stability_config = apikey_stability
        self.stability_base_url = "https://api.stability.ai/v2beta/stable-image/generate/sd3"
        self.stability_base_model = 'sd3-large-turbo'

        # SCENEARIO KEY
        self.scenario_config = apikey_sceneario
        self.scenario_model_landscape = 'asdfasdagadfgadsdga'  # It's one of our signature public models
        self.scenario_model_character = 'asgdasdgasdfadsfhfgfhjfgh' # The "Olivia" model generates images that showcase a character in various thematic settings        
        self.scenario_base_url = "https://api.cloud.scenario.com/v1"

        # MISTRAL KEY
        os.environ["MISTRAL_API_KEY"] = apikey_mistral
        
        # GOOGLE KEY
        os.environ["GOOGLE_API_KEY"] = apikey_google

        # OPEN ROUTER
        self.apikey_openrouter = apikey_openrouter
        os.environ["OPENROUTER_API_KEY"] = self.apikey_openrouter
        
        # ELEVEN LABS KEY
        self.apikey_elevenlabs = apikey_elevenlabs
        os.environ["ELEVENLABS_API_KEY"] = self.apikey_elevenlabs
        
        # LMNT (TTS) KEY
        self.apikey_lmnt = apikey_lmnt
        os.environ["LMNT_API_KEY"] = apikey_lmnt

        # CARTESIA (TTS) KEY
        self.apikey_cartesia = apikey_cartesia
        os.environ["CARTESIA_API_KEY"] = self.apikey_cartesia

        # SPEECHIFY (TTS) KEY
        self.apikey_speechify = apikey_speechify
        os.environ["SPEECHIFY_API_KEY"] = self.apikey_speechify
        
        # PLAY HT (TTS) KEY
        if apikey_playht.find(':') > 0:
            data_playht = apikey_playht.split(':')
            self.apikey_playht_user = data_playht[0]
            self.apikey_playht_password = data_playht[1]
            
        print (" +++API KEYS++++ SET UP SUCCESS!!!")
        
        return jsonify({"success": True})
        
    def set_provider_llm(self):
        args = request.args
        prompt = request.json
        userID = int(prompt["userid"])
        username = prompt["username"]
        password = prompt["password"]
        llm_provider = ProviderLLM(int(prompt["provider"]))
        model_llm = prompt["model"]
        cost_input_token = float(prompt["costinput"])
        cost_input_token = float(prompt["costoutput"])

        print (" +++LLM++++ SET UP PROVIDER TO " + str(llm_provider))
    
        # SPEECH & AUDIO GENERATION
        self.clientOpenAI = OpenAI()
        self.clientElevenLabs = ElevenLabs(api_key = self.apikey_elevenlabs)
    
        # ++++ OPENAI CHATGPT ++++
        if llm_provider == ProviderLLM.CHAT_GPT:
            self.provider_llm = ProviderLLM.CHAT_GPT            
            self.cached_llm = ChatOpenAI(model_name=model_llm)
            self.tokenizer = tiktoken.encoding_for_model(model_llm)
            self.cost_per_token_input = cost_input_token
            self.cost_per_token_output = cost_input_token
            print (" +++LLM++++ Running OpenAI "+ model_llm)
				
        # ++++ MISTRAL ++++
        if llm_provider == ProviderLLM.MISTRAL:
            self.provider_llm = ProviderLLM.MISTRAL
            self.cached_llm = ChatMistralAI(model=model_llm)
            self.tokenizer = MistralTokenizer.from_model(model_llm)
            self.cost_per_token_input = cost_input_token
            self.cost_per_token_output = cost_input_token
            print (" +++LLM++++ Running Mistral "+model_llm)          

        # ++++ DEEPSEEK ++++
        if llm_provider == ProviderLLM.DEEPSEEK:
            self.provider_llm = ProviderLLM.DEEPSEEK
            self.cached_llm = BaseChatOpenAI(model=model_llm, openai_api_key=self.apikey_deepseek, openai_api_base='https://api.deepseek.com', max_tokens=1024)
            self.cost_per_token_input = cost_input_token
            self.cost_per_token_output = cost_input_token
            print (" +++LLM++++ Running Mistral "+model_llm)          

        # ++++ GOOGLE GEMINI ++++
        if llm_provider == ProviderLLM.GOOGLE:
            self.provider_llm = ProviderLLM.GOOGLE
            self.cached_llm = ChatGoogleGenerativeAI(model=model_llm)
            self.tokenizer_google = tokenization.get_tokenizer_for_model("gemini-1.0-pro-002")
            self.cost_per_token_input = cost_input_token
            self.cost_per_token_output = cost_input_token
            print (" +++LLM++++ Running Google "+model_llm)

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
            self.cost_per_token_output = cost_input_token
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
                                  
        self.chainParagraphElevenLabsVoiceSettings = self.promptParagraphElevenLabsVoiceSettings | self.cached_llm | self.parserParagraphElevenLabsVoiceSettings

        self.chainFormatImage = self.promptFormatImage | self.cached_llm | self.parserFormatImage
        self.chainFormatSoundFX = self.promptFormatSoundFX | self.cached_llm | self.parserFormatSoundFX
        self.chainFormatMusicLoop = self.promptFormatMusicLoop | self.cached_llm | self.parserFormatMusicLoop

        self.chainFormatTranslateToken = self.promptFormatTranslateToken | self.cached_llm | self.parserFormatTranslateToken

        return jsonify({"success": True})
        
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

    def count_tokens(self, text):
        if self.provider_llm == ProviderLLM.MISTRAL:
            tokens = self.tokenizer.instruct_tokenizer.tokenizer.encode(text, True, True)
        elif self.provider_llm == ProviderLLM.GOOGLE:
            return self.tokenizer_google.count_tokens(text).total_tokens
        elif self.provider_llm == ProviderLLM.DEEPSEEK:
            return 0
        elif self.provider_llm == ProviderLLM.OPENROUTER:
            return 0
        else:
            tokens = self.tokenizer.encode(text)
        return len(tokens)
    
    def calculate_array_cost(self, input_texts, output_texts):
        if self.is_free_llm():
            return 0
        else:
            input_tokens = sum(self.count_tokens(text) for text in input_texts)
            output_tokens = sum(self.count_tokens(text) for text in output_texts)
        
            total_cost = (input_tokens * self.cost_per_token_input) + (output_tokens * self.cost_per_token_output)
            return total_cost
    
    def calculate_cost(self, input_text, output_text):
        if self.is_free_llm():
            return 0
        else:
            if len(input_text) > 0:
                input_tokens = self.count_tokens(input_text)
            else:
                input_tokens = 0
            output_tokens = self.count_tokens(output_text)

            total_cost = (input_tokens * self.cost_per_token_input) + (output_tokens * self.cost_per_token_output)
            return total_cost    

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

    def get_sceneario_image_url(self, base_url, model_id, inference_id, headers):
        status = ''
        while status not in ['succeeded', 'failed']:
            # Fetch the inference details
            inference_response = requests.get(f'{base_url}/models/{model_id}/inferences/{inference_id}', headers=headers)
            inference_data = inference_response.json()
            # print(inference_data)
            status = inference_data['inference']['status']
            print(f'Inference status: {status}')

            # Wait for a certain interval before polling again
            time.sleep(5)  # Polling every 5 seconds

        # Handle the final status
        if status == 'succeeded':
            print('Inference succeeded!')
            return inference_data['inference']['images'][0]['id'], inference_data['inference']['images'][0]['url']
        else:
            print('Inference failed!')
            print(inference_data)  # Print inference data
            return None, None
            
    def remove_background_image(self, base_url, headers, asset_id):
        url_background = base_url + "/images/erase-background"   
        payload_background = {
            "backgroundColor": "transparent",
            "assetId": asset_id,
            "format": "png"
        }
        response = requests.put(url_background, json=payload_background, headers=headers)
        if response.status_code == 200:
            print('Remove Background succeeded!')
            response_data = response.json()
            print(response_data['asset']['url'])
            return response_data['asset']['url']
        else:
            print('Remove Background failed!')
            print(response)
            return None
        
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
           return self.cached_llm.model + ":CONTEXT[" +  str(self.cached_llm.num_ctx) + "]" # ":GPU["+str(self.cached_llm.num_gpu)  +"]:TEMPERATURE["+ str(self.cached_llm.temperature)+"]"
           
    def question(self):
            args = request.args
            prompt = request.json
            userID = int(prompt["userid"])
            username = prompt["username"]
            password = prompt["password"]
            conversationName = prompt["conversationid"]
            question = prompt["question"]
            chain = bool(prompt["chain"])

            if args.get("debug", default=False, type=bool):
                print("AI question received...")
                print("AI question is {}".format(question))

            self.init_sql_functions(username)

            if self.enable_user_check and not self.sqlFunctions.login_user_id(userID, username, password, self.port_number):
                return "Error: No matching user and password"            

            historyJSON = None
            memory = None
            response = None
            cost = 0
            if chain:
                if not self.sqlFunctions.exist_value(conversationName):
                    self.sqlFunctions.store_new_value(conversationName, "")
            
                historyJSON = self.sqlFunctions.get_history_by_name(conversationName)
                memory = ConversationBufferMemory(return_messages=True)            
            
                if len(historyJSON) > 1:
                    messages = self.sqlFunctions.get_list_messages(historyJSON)
                    for user_msg, ai_msg in messages:
                        memory.chat_memory.add_user_message(user_msg)
                        memory.chat_memory.add_ai_message(ai_msg)
                    
                PROMPT = PromptTemplate(input_variables=["history", "input"], template=self.template_question)
                conversation = ConversationChain(prompt=PROMPT, llm=self.cached_llm, verbose=True, memory=memory)
                jsonResponse = conversation.invoke(question)
                response = self.sqlFunctions.get_ai_message_content(jsonResponse)
                
                input_texts = [msg.content for msg in conversation.memory.buffer if msg.type == "human"]
                output_texts = [msg.content for msg in conversation.memory.buffer if msg.type == "ai"]
                self.store_last_operation_cost(username + "_cost", 0)
                
                historyUpdated = self.sqlFunctions.add_new_message(historyJSON, question, response)
            
                self.sqlFunctions.update_value(conversationName, historyUpdated)
            else:
                response = self.cached_llm.invoke(question)
                if isinstance(response, AIMessage):
                    response = response.content
                    self.store_last_operation_cost(username + "_cost", self.calculate_cost(question, str(response)))

            print (response)

            if args.get("debug", default=False, type=bool):
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
    
    def translation_text(self):
            args = request.args
            prompt = request.json
            userID = int(prompt["userid"])
            username = prompt["username"]
            password = prompt["password"]
            conversationName = prompt["conversationid"]
            instructions = prompt["instructions"]
            question = prompt["question"]
            chain = bool(prompt["chain"])
            isjson = bool(prompt["isjson"])

            if args.get("debug", default=False, type=bool):
                print("AI translation received...")
                print("AI translation instructions are {}".format(instructions))
                print("AI translation original text is {}".format(question))

            self.init_sql_functions(username)

            if self.enable_user_check and not self.sqlFunctions.login_user_id(userID, username, password, self.port_number):
                return "Error: No matching user and password"            

            if chain:
                if not self.sqlFunctions.exist_value(conversationName):
                    self.sqlFunctions.store_new_value(conversationName, "")
            
                historyJSON = self.sqlFunctions.get_history_by_name(conversationName)
                memory = ConversationBufferMemory(return_messages=True)            
            
                if len(historyJSON) > 1:
                    messages = self.sqlFunctions.get_list_messages(historyJSON)
                    for user_msg, ai_msg in messages:
                        memory.chat_memory.add_user_message(user_msg)
                        memory.chat_memory.add_ai_message(ai_msg)
                
                promptTranslationChain = PromptTemplate(template=self.templateTranslation, input_variables=["history", "input"])
                conversation = ConversationChain(prompt=promptTranslationChain, llm=self.cached_llm, verbose=True, memory=memory)
                jsonResponse = None
                if len(instructions) > 0:
                    jsonResponse = conversation.invoke(instructions + " " + question)
                else:
                    jsonResponse = conversation.invoke(question)                
                response = self.sqlFunctions.get_ai_message_content(jsonResponse)
                
                input_texts = [msg.content for msg in conversation.memory.buffer if msg.type == "human"]
                output_texts = [msg.content for msg in conversation.memory.buffer if msg.type == "ai"]
                cost = self.calculate_array_cost(input_texts, output_texts)
                
                historyUpdated = self.sqlFunctions.add_new_message(historyJSON, question, response)
            
                self.sqlFunctions.update_value(conversationName, historyUpdated)
            else:
                if isjson == True:
                    response = self.chainFormatTranslateToken.invoke({"query": question})
                    self.store_last_operation_cost(username + "_cost", self.calculate_cost(question, str(response)))
                else:
                    response = None
                    if len(instructions) > 0:
                        response = self.cached_llm.invoke(instructions + " " + question)
                    else:
                        response = self.cached_llm.invoke(question)
                    if isinstance(response, AIMessage):
                        response = response.content
                        self.store_last_operation_cost(username + "_cost", self.calculate_cost(question, str(response)))
                    # print(response)

            if args.get("debug", default=False, type=bool):
                    print("AI translation response received...")
                    print(response)

            return response


    # ++++++++++++++++++++++
    # ++++++++++++++++++++++
    # FORMAT VISUAL
    # ++++++++++++++++++++++
    # ++++++++++++++++++++++    
    def format_image_generation(self):
            args = request.args
            prompt = request.json
            userID = int(prompt["userid"])
            username = prompt["username"]
            password = prompt["password"]
            conversationName = prompt["conversationid"]
            question = prompt["question"]

            self.init_sql_functions(username)

            if args.get("debug", default=False, type=bool):
                print("AI format visual image creation received...")
                print("AI format visual image creation is {}".format(question))

            if self.enable_user_check and not self.sqlFunctions.login_user_id(userID, username, password, self.port_number):
                return "Error: No matching user and password"            

            try:
                response = self.chainFormatImage.invoke({"query": question})
            except OutputParserException as e:
                if self.provider_llm == ProviderLLM.ANTHROPIC:
                    response = self.extract_json_from_string(e.llm_output)

            self.store_last_operation_cost(username + "_cost", self.calculate_cost(question, str(response)))
            # print(response)

            if args.get("debug", default=False, type=bool):
                    print("AI format visual image creation received...")
                    print(response)

            return response

    def format_soundfx_generation(self):
            args = request.args
            prompt = request.json
            userID = int(prompt["userid"])
            username = prompt["username"]
            password = prompt["password"]
            conversationName = prompt["conversationid"]
            question = prompt["question"]

            self.init_sql_functions(username)

            if args.get("debug", default=False, type=bool):
                print("AI format SOUND FX creation received...")
                print("AI format SOUND FX creation is {}".format(question))

            if self.enable_user_check and not self.sqlFunctions.login_user_id(userID, username, password, self.port_number):
                return "Error: No matching user and password"            

            try:
                response = self.chainFormatSoundFX.invoke({"query": question})
            except OutputParserException as e:
                if self.provider_llm == ProviderLLM.ANTHROPIC:
                    response = self.extract_json_from_string(e.llm_output)

            self.store_last_operation_cost(username + "_cost", self.calculate_cost(question, str(response)))
            # print(response)

            if args.get("debug", default=False, type=bool):
                    print("AI format SOUND FX creation received...")
                    print(response)

            return response

    def format_musicloop_generation(self):
            args = request.args
            prompt = request.json
            userID = int(prompt["userid"])
            username = prompt["username"]
            password = prompt["password"]
            conversationName = prompt["conversationid"]
            question = prompt["question"]

            self.init_sql_functions(username)

            if args.get("debug", default=False, type=bool):
                print("AI format MUSIC LOOP creation received...")
                print("AI format MUSIC LOOP creation is {}".format(question))

            if self.enable_user_check and not self.sqlFunctions.login_user_id(userID, username, password, self.port_number):
                return "Error: No matching user and password"            

            try:
                response = self.chainFormatMusicLoop.invoke({"query": question})
            except OutputParserException as e:
                if self.provider_llm == ProviderLLM.ANTHROPIC:
                    response = self.extract_json_from_string(e.llm_output)

            self.store_last_operation_cost(username + "_cost", self.calculate_cost(question, str(response)))
            # print(response)

            if args.get("debug", default=False, type=bool):
                    print("AI format MUSIC LOOP creation received...")
                    print(response)

            return response


    # ++++++++++++++++++++++
    # ++++++++++++++++++++++
    # DATABASE ENDPOINTS
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
           
    def new_conversation(self):
            args = request.args        
            userID = args.get("userid", default="", type=int)
            username = args.get("username", default="", type=str)
            password = args.get("password", default="", type=str)
            nameScript = args.get("namescript", default="None", type=str)

            self.init_sql_functions(username)

            if self.enable_user_check and not self.sqlFunctions.login_user_id(userID, username, password, self.port_number):
                print ("Error: No matching user and password")
                return jsonify({"success": False})

            if not self.sqlFunctions.exist_value(nameScript):
                self.sqlFunctions.store_new_value(nameScript, "")

            print("New conversation with name("+nameScript+")")

            return jsonify({"success": True, "conversation_id": nameScript})

    def get_conversation(self):
            args = request.args        
            userID = args.get("userid", default="", type=int)
            username = args.get("username", default="", type=str)
            password = args.get("password", default="", type=str)
            conversationID = args.get("conversationid", default="", type=str)

            self.init_sql_functions(username)
            
            if self.enable_user_check and not self.sqlFunctions.login_user_id(userID, username, password, self.port_number):
                print ("Error: No matching user and password")
                return jsonify({"success": False})
            
            historyJSON = self.sqlFunctions.get_history_by_name(conversationID)
            
            return historyJSON

    def delete_conversation(self):
            args = request.args        
            userID = args.get("userid", default="", type=int)
            username = args.get("username", default="", type=str)
            password = args.get("password", default="", type=str)
            conversationID = args.get("conversationid", default="", type=str)

            self.init_sql_functions(username)

            if self.enable_user_check and not self.sqlFunctions.login_user_id(userID, username, password, self.port_number):
                print ("Error: No matching user and password")
                return jsonify({"success": False})
            
            self.sqlFunctions.delete_value_by_name(conversationID)
            
            print("Conversation deleted with name("+conversationID+")")

            return jsonify({"success": True})        
    
    def delete_all_conversations(self):
            args = request.args        
            userID = args.get("userid", default="", type=int)
            username = args.get("username", default="", type=str)
            password = args.get("password", default="", type=str)
            conversationIDs = args.get("conversationids", default="", type=str)

            self.init_sql_functions(username)

            if self.enable_user_check and not self.sqlFunctions.login_user_id(userID, username, password, self.port_number):
                print ("Error: No matching user and password")
                return jsonify({"success": False})

            if args.get("debug", default=False, type=bool):
                    print("+++++++++++++userName["+username+"] Conversations to delete("+conversationIDs+")")

            ids = conversationIDs.split(',')

            for convID in ids:
                    if len(convID) > 0:
                        if args.get("debug", default=False, type=bool):
                            print("Deleting conversation with conversation ID("+convID+")")

                        self.sqlFunctions.delete_value_by_name(convID)
                        
            return jsonify({"success": True})

    def get_voice_id_by_name(self, voices, name):
        for voice in voices:
            voice_name = voice.get("name")
            # print ("Voice Name="+ voice_name + " against name=" + name)
            if voice_name == name:
                return voice.get("voice_id")
        return None

    def get_lmnt_voice_id_by_name(self, name):
        url = "https://api.lmnt.com/v1/ai/voice/list"
        headers = {"X-API-Key": self.apikey_lmnt}

        try:
            # Make the GET request
            response = requests.get(url, headers=headers)

            # Check for successful response
            if response.status_code != 200:
                print(f"Error: Received status code {response.status_code}")
                return None

            # Parse the JSON response
            voice_list = response.json()

            # Search for the matching name
            for voice in voice_list:
                if voice.get("name") == name:
                    return voice.get("id")

            # If no match is found, return None
            print(f"No voice found with name: {name}")
            return None

        except Exception as e:
            print(f"An error occurred: {e}")
            
        return None

    def get_cartesia_voice_id_by_name(self, name):
        url = "https://api.cartesia.ai/voices/"
        headers = {
            "Cartesia-Version": "2024-06-10",
            "X-API-Key": self.apikey_cartesia
        }
        try:
            # Make the GET request
            response = requests.get(url, headers=headers)

            # Check for successful response
            if response.status_code != 200:
                print(f"Error: Received status code {response.status_code}")
                return None

            # Parse the JSON response
            voice_list = response.json()

            # Search for the matching name
            # print(f"voice_list: {voice_list}")
            for voice in voice_list:
                if voice.get("name") == name:
                    return voice.get("id")

            # If no match is found, return None
            print(f"No voice found with name: {name}")
            return None

        except Exception as e:
            print(f"An error occurred: {e}")
            
        return None

    def get_speechify_voice_id_by_name(self, name):
        url = "https://api.sws.speechify.com/v1/voices"
        headers = {
            "accept": "*/*",
            "Authorization": f"Bearer {self.apikey_speechify}"
        }
        try:
            # Make the GET request
            response = requests.get(url, headers=headers)

            # Check for successful response
            if response.status_code != 200:
                print(f"Error: Received status code {response.status_code}")
                return None

            # Parse the JSON response
            voice_list = response.json()

            # Search for the matching name
            # print(f"voice_list: {voice_list}")
            for voice in voice_list:
                if voice.get("display_name") == name:
                    return voice["id"]

            # If no match is found, return None
            print(f"No voice found with name: {name}")
            return None

        except Exception as e:
            print(f"An error occurred: {e}")
            
        return None

    def find_closest_emotion(self, emotion):
        primary_emotions = {
            "angry": ["mad", "furious", "irritated", "annoyed", "displeased"],
            "cheerful": ["happy", "joyful", "content", "delighted", "excited", "hopeful"],
            "sad": ["unhappy", "depressed", "melancholy", "miserable"],
            "terrified": ["petrified", "horrified", "panicked", "dreadful"],
            "relaxed": ["calm", "peaceful", "serene", "tranquil", "casual", "thoughtful", "reflective"],
            "fearful": ["scared", "anxious", "nervous", "worried"],
            "surprised": ["shocked", "amazed", "astonished", "startled", "curious", "questioning"],
            "calm": ["realistic", "informative", "neutral"],
            "assertive": ["confident", "firm", "decisive", "bold", "determined"],
            "energetic": ["lively", "enthusiastic", "active", "vibrant", "dynamic"],
            "warm": ["affectionate", "kind", "friendly", "caring", "compassionate"],
            "direct": ["straightforward", "honest", "blunt", "frank", "unambiguous"],
            "bright": ["radiant", "vivid", "shiny", "sparkling", "luminous"],
        }

        emotion = emotion.lower()

        if emotion in primary_emotions:
            return emotion

        for main_emotion, synonyms in primary_emotions.items():
            if emotion in synonyms:
                if main_emotion == "neutral":
                    return ""
                else:
                    return main_emotion
        
        return ""
        
    def get_playht_id_by_name(self, name):
        url = "https://api.play.ht/api/v2/cloned-voices"
        headers = {
            "AUTHORIZATION": f"Bearer {self.apikey_playht_password}",
            "X-USER-ID": self.apikey_playht_user
        }
        try:
            # Make the GET request
            response = requests.get(url, headers=headers)

            # Check for successful response
            if response.status_code != 200:
                print(f"Error: Received status code {response.status_code}")
                return None

            # Parse the JSON response
            voice_list = response.json()
    
            for entry in voice_list:
                if entry.get("name") == name:
                    return entry.get("id")
            return None

            # If no match is found, return None
            print(f"No voice found with name: {name}")
            return None

        except Exception as e:
            print(f"An error occurred: {e}")
            
        return None
    
    def find_existing_emotion(self, target_emotion):
        available_emotions = {
            "female_happy", "female_sad", "female_angry", "female_fearful",
            "female_disgust", "female_surprised", "male_happy", "male_sad",
            "male_angry", "male_fearful", "male_disgust", "male_surprised"
        }

        target_emotion = target_emotion.lower()

        if target_emotion in available_emotions:
            return target_emotion
        else:
            return ""

    def get_final_language(self, language_code: str) -> str:
        language_map = {
            "en": "ENGLISH",
            "es": "SPANISH",
            "de": "GERMAN",
            "fr": "FRENCH",
            "it": "ITALIAN",
            "ru": "RUSSIAN",
            "ca": "CATALAN"
        }
        return language_map.get(language_code, "ENGLISH")
        
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
            project = prompt["project"]
            voice = prompt["voice"]
            speech = prompt["speech"]
            language = prompt["language"]
            emotion = prompt["emotion"]
            speed = prompt["speed"]
            provider = int(prompt["provider"])
            stability = float(prompt["stability"])
            similarity_boost = float(prompt["similarity"])
            style = float(prompt["style"])

            self.init_sql_functions(username)

            if self.enable_user_check and not self.sqlFunctions.login_user_id(userID, username, password, self.port_number):
                return "Error: No matching user and password"            

            print ("SPEECH GENERATION::PROVIDER["+str(provider)+"]::TEXT="+speech+"::SETTINGS("+str(stability)+","+str(similarity_boost)+","+str(style)+")")

            try:
                # //////// XTTS PROVIDER ////////
                if provider == 0:
                    # Define the URL and the payload to send.
                    url = self.url_speech_generation

                    payload = {
                        "project": project,
                        "username": username,
                        "voice": voice,
                        "speech": speech,
                        "language": language,
                        "emotion": emotion,
                        "speed": speed
                    }

                    # Send said payload to said URL through the API.
                    response = requests.post(url=f'{url}/ai/speech', json=payload)
                    return response.content

                # //////// ELEVENLABS PROVIDER ////////
                elif provider == 1: 
                    finalVoice = project + "_" + voice
                    print ("ElevenLabs::speech_generation::finalVoice="+ finalVoice)
                    if len(voice) == 0:
                        finalVoiceID = project
                    else:
                        allvoices = self.clientElevenLabs.voices.get_all()
                        voice_data = allvoices.dict()
                        array_voices = voice_data.get("voices")
                        finalVoiceID = self.get_voice_id_by_name(array_voices, finalVoice)                
                    # print ("ElevenLabs::speech_generation::Voice ID="+ finalVoiceID)
                    if finalVoiceID is not None:
                        temp_mp3_eleven_file = "outputEleven_"+str(userID)+".mp3"
                        responseElevenLabs = self.clientElevenLabs.text_to_speech.convert(
                            text=speech,
                            voice_id=finalVoiceID,
                            voice_settings=VoiceSettings(stability=stability, similarity_boost=similarity_boost, style=style, use_speaker_boost=True),
                            model_id="eleven_multilingual_v2"
                        )
                        save(responseElevenLabs, temp_mp3_eleven_file)
                        dataaudio = AudioSegment.from_mp3(temp_mp3_eleven_file).export(format="ogg")
                        os.remove(temp_mp3_eleven_file)
                        return dataaudio

                # //////// OPENAI PROVIDER ////////
                elif provider == 2:
                    temp_mp3_openai_file = "outputOpenAI_"+str(userID)+".mp3"
                    responseOpenAI = self.clientOpenAI.audio.speech.create(
                                                                model="tts-1",
                                                                voice="alloy",
                                                                input=speech
                                                                )
                    responseOpenAI.stream_to_file(temp_mp3_openai_file)
                    dataaudio = AudioSegment.from_mp3(temp_mp3_openai_file).export(format="ogg")
                    os.remove(temp_mp3_openai_file)
                    return dataaudio   
                # //////// LMNT PROVIDER ////////
                elif provider == 3:
                    url = "https://api.lmnt.com/v1/ai/speech"

                    if len(voice) == 0:
                        finalID = project
                    else:
                        finalVoice = project + "_" + voice
                        finalVoice = finalVoice.replace(" ", "")
                        finalID = self.get_lmnt_voice_id_by_name(finalVoice)

                    # print ("LMNT::speech_generation::Voice ID="+ finalID)
                    if finalID is not None:
                        # Define the form data
                        form_data = {
                            "voice": finalID,
                            "text": speech,
                            "language": language,
                            "model": "blizzard",
                            "format": "mp3",
                            "return_durations": "true",
                            "seed": "123"
                        }

                        # Define headers
                        headers = {
                            # "Content-Type": "multipart/form-data",
                            "X-API-Key": self.apikey_lmnt
                        }

                        # Make the POST request
                        response = requests.post(url, headers=headers, data=form_data)

                        # Print the response
                        print(response.status_code)
                        if response.status_code == 200:
                            # print("LMNT:Audio generation successfull")
                            
                            response_data = response.json()

                            # Extract and decode the base64 audio data
                            audio_base64 = response_data.get("audio")
                            if audio_base64:
                                audio_data = base64.b64decode(audio_base64)
                                
                                tmp_lmnt_audio_mp3 = "lmnt-audio-output_"+str(userID)+".mp3"
                                with open(tmp_lmnt_audio_mp3, "wb") as audio_file:
                                    audio_file.write(audio_data)
                                    
                                dataaudio = AudioSegment.from_mp3(tmp_lmnt_audio_mp3).export(format="ogg")
                                os.remove(tmp_lmnt_audio_mp3)
                                
                                return dataaudio                        
                            else:
                                print("Error:Audio data not found in the response.")
                        else:
                            print(f"Error: {response.status_code}")
                            print(response.text)

                # //////// CARTESIA PROVIDER ////////
                elif provider == 4:
                    url = "https://api.cartesia.ai/tts/bytes"

                    if len(voice) == 0:
                        finalID = project
                    else:
                        finalVoice = project + "_" + voice
                        finalID = self.get_cartesia_voice_id_by_name(finalVoice)

                    # print ("Cartesia::speech_generation::Voice ID="+ finalID)
                    if finalID is not None:
                        payload = {
                            "model_id": "sonic-english",
                            "transcript": speech,
                            "voice": {
                                "mode": "id",
                                "id": finalID
                            },
                            "output_format": {
                                "container": "mp3",
                                "bit_rate": 128000,
                                "sample_rate": 44100
                            },
                            "language": language
                        }
                        headers = {
                            "Cartesia-Version": "2024-06-10",
                            "X-API-Key": self.apikey_cartesia,
                            "Content-Type": "application/json"
                        }
                        response = requests.post(url, json=payload, headers=headers)

                        # Print the response
                        print(response.status_code)
                        if response.status_code == 200:
                            # print("Cartesia:Audio generation successfull")
                            
                            tmp_cartesia_audio_mp3 = "cartesia-audio-output_"+str(userID)+".mp3"
                            with open(tmp_cartesia_audio_mp3, "wb") as audio_file:
                                audio_file.write(response.content)
                                
                            dataaudio = AudioSegment.from_mp3(tmp_cartesia_audio_mp3).export(format="ogg")
                            os.remove(tmp_cartesia_audio_mp3)
                            
                            return dataaudio                        
                        else:
                            print(f"Error: {response.status_code}")
                            print(response.text)
                            
                            
                # //////// SPEECHIFY PROVIDER ////////
                elif provider == 5:
                    url = "https://api.sws.speechify.com/v1/audio/speech"

                    if len(voice) == 0:
                        finalID = project
                    else:
                        finalVoice = project + "_" + voice
                        finalID = self.get_speechify_voice_id_by_name(finalVoice)

                    final_emotion = self.find_closest_emotion(emotion)

                    text_to_speech = "<speak>" + speech + "</speak>"
                    if (len(final_emotion) > 0):
                        text_to_speech = "<speak>"
                        text_to_speech += "<speechify:style emotion=\""+final_emotion+"\">"
                        text_to_speech += speech
                        text_to_speech += "</speechify:style>"
                        text_to_speech += "</speak>"

                    # print ("Speechify::speech_generation::ID="+ finalID + "::text_to_speech=" + text_to_speech)                    
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

                        # Print the response
                        print(response.status_code)
                        if response.status_code == 200:
                            # print("Speechify:Audio generation successfull")

                            response_data = response.json()

                            # Extract and decode the base64 audio data
                            audio_base64 = response_data.get("audio_data")
                            if audio_base64:
                                audio_data = base64.b64decode(audio_base64)
                            
                                tmp_speechify_audio_mp3 = "speechify-audio-output_"+str(userID)+".mp3"
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
                        
                # //////// PLAY HT PROVIDER ////////
                elif provider == 6:
                    url = "https://api.play.ht/api/v2/tts"
                
                    if len(voice) == 0:
                        finalID = project
                    else:
                        finalVoice = project + "_" + voice
                        finalID = self.get_playht_id_by_name(finalVoice)

                    # print ("PlayHT::speech_generation::Voice ID="+ finalID)
                    if finalID is not None:
                        headers = {
                            "AUTHORIZATION": f"Bearer {self.apikey_playht_password}",
                            "X-USER-ID": self.apikey_playht_user,
                        }
                        payload = {
                            "voice_engine": "PlayHT2.0",
                            "text": speech,
                            "voice": finalID,
                            "output_format": "ogg",
                            "sample_rate": 44100
                        }
                        finalEmotion = self.find_existing_emotion(emotion)
                        if (len(finalEmotion) > 0):
                            payload = {
                                "voice_engine": "PlayHT2.0",
                                "text": speech,
                                "voice": finalID,
                                "output_format": "ogg",
                                "sample_rate": 44100,
                                "emotion": finalEmotion
                            }
                        response = requests.request("POST", url, json=payload, headers=headers)

                        # Print the response
                        print(response.status_code)
                        if response.status_code == 200:
                            # print("PlayHT:Audio generation successfull")                            
                            
                            final_url = ""
                            for line in response.iter_lines():
                                if line:
                                    decoded_line = line.decode("utf-8").strip()
                                    
                                    # Only process lines starting with "data:"
                                    if decoded_line.startswith("data:"):
                                        try:
                                            import json
                                            event_data = json.loads(decoded_line[5:].strip())  # Remove "data: " and parse JSON
                                            
                                            # Print progress updates
                                            # print(f"Progress: {event_data.get('progress', 0)}, Stage: {event_data.get('stage', '')}")

                                            # Check if we reached the final response
                                            if event_data.get("stage") == "complete":
                                                final_url = event_data.get("url")
                                                # print(f"Processing complete! Download URL: {final_url}")
                                                break  # Exit loop after getting final result

                                        except json.JSONDecodeError:
                                            print("Error decoding JSON:", decoded_line)     
                                                                        
                            # print("PlayHT:Audio generation successfull::URL=" + final_url)                            
                            if len(final_url) > 0:
                                response_download = requests.get(final_url)
                            
                                if response_download.status_code == 200:
                                    print("PlayHT:Downloaded Audio successfull")
                                    return response_download.content                
                                else:
                                    print(f"Error: {response_download.status_code}")
                                    print(response_download.text)
                            else:
                                return "Error: No URL sound created"
                        else:
                            print(f"Error: {response.status_code}")
                            print(response.text)
                        
                else:
                    return "Error: Invalid provider"
            except Exception as ex:
                return "Error: Exception " + str(ex)
    
    def upload_speech_voice(self):            
            userID = request.form.get("userid")
            username = request.form.get("username")
            password = request.form.get("password")
            project = request.form.get("project")
            voicename = request.form.get("voice")
            language = request.form.get("language")
            provider = int(request.form.get("provider"))
            description = request.form.get("description")
            voicedata = request.files.get("file")            

            self.init_sql_functions(username)

            if self.enable_user_check and not self.sqlFunctions.login_user_id(userID, username, password, self.port_number):
                print ("Error: No matching user and password")
                return jsonify({"success": False})

            print ("VOICE UPLOAD::PROVIDER["+str(provider)+"]::VOICE="+voicename)

            # If the user does not select a file, the browser submits an empty file without a filename.
            if voicedata.filename == '':
                flash('No selected file')
                return jsonify({"success": False})
                
            if voicedata:
                # //////// XTTS PROVIDER ////////
                if provider == 0:
                    url = self.url_speech_generation

                    # Prepare the payload and files for the request
                    payload = {
                        "project": project,
                        "voice": voicename,
                        "username": username,
                        "language": language
                    }
                    files = {
                        "file": (voicedata.filename, voicedata.read(), voicedata.mimetype)
                    }

                    # Send the payload and files to the endpoint
                    response = requests.post(url=f'{url}/ai/speech/voice', data=payload, files=files)

                    if response.status_code == 200:
                        return jsonify({"success": True, "response": response.json()})
                    else:
                        return jsonify({"valid": False, "error": "Error cloning voice"}), 401
                    
                # //////// ELEVENLABS PROVIDER ////////
                elif provider == 1: 
                    finalVoice = project + "_" + voicename
                    allvoices = self.clientElevenLabs.voices.get_all()
                    voice_data = allvoices.dict()
                    array_voices = voice_data.get("voices")                    
                    if self.get_voice_id_by_name(array_voices,finalVoice) is None: 
                        print ("ElevenLabs::UploadVoice::Adding voice="+finalVoice)
                        temp_file_path = "./temp_uploaded_voice_"+str(userID)+".ogg"
                        voicedata.save(temp_file_path)
                        response = self.clientElevenLabs.clone(
                            name=finalVoice,
                            description=description,
                            files=[temp_file_path],
                        )
                        os.remove(temp_file_path)
                        return jsonify({"success": True, "response": response.json()})
                        
                # //////// OPENAI PROVIDER ////////                        
                elif provider == 2:                    
                    return jsonify({"success": False})
                    
                # //////// LMNT PROVIDER ////////                        
                elif provider == 3:
                    url_lmnt = "https://api.lmnt.com/v1/ai/voice"

                    finalVoice = project + "_" + voicename
                    finalVoice = finalVoice.replace(" ", "")

                    finalID = self.get_lmnt_voice_id_by_name(finalVoice)

                    if finalID is None: 
                        # Convert the OGG file to MP3
                        ogg_audio = AudioSegment.from_file(voicedata, format="ogg")
                        mp3_buffer = io.BytesIO()
                        ogg_audio.export(mp3_buffer, format="mp3")
                        mp3_buffer.seek(0)  # Reset the buffer position to the beginning
                    
                        metadata = {
                            "name": finalVoice,
                            "type": "instant",
                            "enhance": False
                        }
                        
                        headers = {
                            "X-API-Key": self.apikey_lmnt
                        }
                        files = {
                            'files': ('converted.mp3', mp3_buffer, 'audio/mpeg')
                        }
                        data = {
                            "metadata": json.dumps(metadata)
                        }

                        response = requests.post(url_lmnt, headers=headers, files=files, data=data)
                        print("LMNT::response.status_code=" + str(response.status_code))
                        print("LMNT::response.text=" + response.text)

                        if response.status_code == 200:
                            return jsonify({"success": True, "response": response.text})              
                        else:
                            return jsonify({"valid": False, "error": "Error cloning voice"}), 401
                    
                # //////// CARTESIA PROVIDER ////////                            
                elif provider == 4:
                    url = "https://api.cartesia.ai/voices/clone"
                    
                    files = { "clip": voicedata }
                    
                    finalVoice = project + "_" + voicename
                    
                    finalID = self.get_cartesia_voice_id_by_name(finalVoice)
                    
                    if finalID is None:
                        payload = {
                            "name": finalVoice,
                            "description": description,
                            "language": language,
                            "mode": "stability",
                            "enhance": "true"
                        }
                        headers = {
                            "Cartesia-Version": "2024-06-10",
                            "X-API-Key": self.apikey_cartesia
                        }
                        response = requests.post(url, data=payload, files=files, headers=headers)                    
                        print("Cartesia::response.status_code=" + str(response.status_code))
                        print("Cartesia::response.text=" + response.text)

                        if response.status_code == 200:
                            return jsonify({"success": True, "response": response.text})
                        else:
                            return jsonify({"valid": False, "error": "Error cloning voice"}), 401
                
                # //////// SPEECHIFY PROVIDER ////////                            
                elif provider == 5:
                    url = "https://api.sws.speechify.com/v1/voices"
                    
                    files = { "sample": voicedata }
                    
                    finalVoice = project + "_" + voicename
                    
                    finalID = self.get_speechify_voice_id_by_name(finalVoice)
                    
                    if finalID is None:
                        consent_data = {
                            "fullName": voicename,
                            "email": description
                        }
                        # print("SPEECHIFY::consent_data=" + str(consent_data))
                        payload = {
                            "name": finalVoice,
                            "consent": json.dumps(consent_data),
                            "language": language
                        }
                        headers = {
                            "Authorization": f"Bearer {self.apikey_speechify}"
                        }
                        response = requests.post(url, data=payload, files=files, headers=headers)                    
                        print("SPEECHIFY::response.status_code=" + str(response.status_code))
                        print("SPEECHIFY::response.text=" + response.text)

                        if response.status_code == 200:
                            return jsonify({"success": True, "response": response.text})
                        else:
                            return jsonify({"valid": False, "error": "Error cloning voice"}), 401
                
                # //////// PLAYHT PROVIDER ////////                            
                elif provider == 6:
                    url = "https://api.play.ht/api/v2/cloned-voices/instant"
                    
                    finalVoice = project + "_" + voicename
                    
                    finalID = self.get_playht_id_by_name(finalVoice)
                    
                    if finalID is None:
                        files = {
                            "voice_name": (None, finalVoice),
                            "sample_file": ("sample.wav", voicedata, "audio/ogg")
                        }
                        headers = {
                            "accept": "application/json",
                            "AUTHORIZATION": f"Bearer {self.apikey_playht_password}",
                            "X-USER-ID": self.apikey_playht_user,
                        }
                        print("PLAYHT::headers=" + str(headers))
                        response = requests.post(url, files=files, headers=headers)                    
                        print("PLAYHT::response.status_code=" + str(response.status_code))
                        print("PLAYHT::response.text=" + response.text)

                        if response.status_code == 201:
                            return jsonify({"success": True, "response": response.text})
                        else:
                            return jsonify({"valid": False, "error": "Error cloning voice"}), 401
                
            return jsonify({"success": False})

    def audio_generation(self) -> bytes:
            args = request.args
            prompt = request.json
            userID = int(prompt["userid"])
            username = prompt["username"]
            password = prompt["password"]
            description = prompt["description"]
            duration = int(prompt["duration"])
            provider = int(prompt["provider"])

            self.init_sql_functions(username)

            if self.enable_user_check and not self.sqlFunctions.login_user_id(userID, username, password, self.port_number):
                return "Error: No matching user and password"            

            if provider == 1:
                temp_mp3_eleven_file = "outputFXEleven_"+str(userID)+".mp3"
                responseElevenLabs = self.clientElevenLabs.text_to_sound_effects.convert( text=description, duration_seconds=duration )
                save(responseElevenLabs, temp_mp3_eleven_file)
                dataaudio = AudioSegment.from_mp3(temp_mp3_eleven_file).export(format="ogg")
                os.remove(temp_mp3_eleven_file)
                return dataaudio
            else:
                # Define the URL and the payload to send.
                url = self.url_audio_generation

                payload = {
                    "description": description,
                    "duration": duration
                }

                # Send said payload to said URL through the API.
                response = requests.post(url=f'{url}/ai/audio', json=payload)
                return response.content
    
    def music_generation(self) -> bytes:
            args = request.args
            prompt = request.json
            userID = int(prompt["userid"])
            username = prompt["username"]
            password = prompt["password"]
            description = prompt["description"]
            duration = int(prompt["duration"])

            self.init_sql_functions(username)

            if self.enable_user_check and not self.sqlFunctions.login_user_id(userID, username, password, self.port_number):
                return "Error: No matching user and password"            

            # Define the URL and the payload to send.
            url = self.url_audio_generation

            payload = {
                "description": description,
                "duration": duration
            }

            # Send said payload to said URL through the API.
            response = requests.post(url=f'{url}/ai/music', json=payload)
            return response.content

    # ++ endpoint POST "/ai/image" ++
    # Raw body:
    # {
    #    "userid": -1,
    #    "username": "username",
    #    "password": "password",
    #    "description": "A dog enjoying a chicken bone",
    #    "exclude": "",
    #    "steps": 50,
    #    "width": 512,
    #    "height": 512
    # }
    def image_generation(self) -> bytes:
            args = request.args
            prompt = request.json
            userID = int(prompt["userid"])
            username = prompt["username"]
            password = prompt["password"]
            provider = prompt["provider"]
            description = prompt["description"]
            exclude = prompt["exclude"]
            steps = int(prompt["steps"])
            width = int(prompt["width"])
            height = int(prompt["height"])
            # data = request.files['file'].read()

            self.init_sql_functions(username)

            if self.enable_user_check and not self.sqlFunctions.login_user_id(userID, username, password, self.port_number):
                return "Error: No matching user and password"            

            if provider == 1:
                # Define the URL and the payload to send.
                url = self.url_image_generation

                payload = {
                    "prompt": description,
                    "negative_prompt": exclude,
                    "steps": steps,
                    "width": width,
                    "height": height
                }

                self.store_last_operation_cost(username + "_cost", 0)

                # Send said payload to said URL through the API.
                response = requests.post(url=f'{url}/sdapi/v1/txt2img', json=payload)
                r = response.json()
                # f.write(base64.b64decode(r['images'][0]))
                return base64.b64decode(r['images'][0])
            elif provider == 2:
                clientImagesOpenAI = OpenAI()
                response = clientImagesOpenAI.images.generate(
                                                      model="dall-e-2",
                                                      prompt=description,
                                                      size=str(width) + "x" + str(height),
                                                      quality="standard",
                                                      n=1
                                                    )
                print (response.data[0].url)
                # DALL-E 2
                # 1024×1024 ($0.020 / image)
                # 512×512 ($0.018 / image)
                # 256×256 ($0.016 / image)
                if width == 256:
                    self.store_last_operation_cost(username + "_cost", 0.016)
                elif width == 512:
                    self.store_last_operation_cost(username + "_cost", 0.018)
                else:
                    self.store_last_operation_cost(username + "_cost", 0.02)
                return requests.get(response.data[0].url).content
            elif provider == 3:
                clientImagesOpenAI = OpenAI()
                response = clientImagesOpenAI.images.generate(
                                                      model="dall-e-3",
                                                      prompt=description,
                                                      size=str(width) + "x" + str(height),
                                                      quality="standard",
                                                      n=1
                                                    )
                print (response.data[0].url)
                # DALL-E 3
                # Standard (1024×1024) $0.040 / image
                # Standard (1024×1792, 1792×1024) $0.080 / image
                # HD (1024×1024) $0.080 / image
                # HD (1024×1792, 1792×1024) $0.120 / image
                if width == 1024 and height == 1024:
                    self.store_last_operation_cost(username + "_cost", 0.04)
                else:
                    self.store_last_operation_cost(username + "_cost", 0.08)
                return requests.get(response.data[0].url).content
            elif provider == 4:
                url_scenario = self.scenario_base_url + "/generate/txt2img"
                model_id = self.scenario_model_landscape
                authorize_scenario = base64.b64encode(self.scenario_config.encode('ascii')).decode('ascii')
                return self.generate_scenario_image(url_scenario, model_id, authorize_scenario, description, width, height, False)
            elif provider == 5:
                url_scenario = self.scenario_base_url + "/generate/txt2img"
                model_id = self.scenario_model_character
                authorize_scenario = base64.b64encode(self.scenario_config.encode('ascii')).decode('ascii')
                return self.generate_scenario_image(url_scenario, model_id, authorize_scenario, description, width, height, True)
            elif provider == 6:
                response = requests.post(
                    self.stability_base_url,
                    headers={
                        "authorization": self.stability_config,
                        "accept": "image/*"
                    },
                    files={"none": ''},
                    data={
                        "prompt": description,
                        "model": self.stability_base_model,     
                        "aspect_ratio": self.closest_aspect_ratio(width, height),
                        "output_format": "png",
                    },
                )

                if response.status_code == 200:
                    return response.content
                else:
                    return None                
            else:
                # Define the URL and the payload to send.
                url = self.url_flux_image_generation

                payload = {
                    "width": width,
                    "height": height,
                    "num_steps": steps,
                    "guidance": 3.5,
                    "seed": -1,
                    "prompt": description,
                    "init_image": None,
                    "image2image_strength": 0.8,
                    "add_sampling_metadata": True                    
                }

                self.store_last_operation_cost(username + "_cost", 0)

                # Send said payload to said URL through the API.
                response = requests.post(url=f'{url}/generate_image', json=payload)
                r = response.json()
                return base64.b64decode(r['generated_image'])
            
    def closest_aspect_ratio(self, width, height):
        # Calculate the aspect ratio
        aspect_ratio = width / height
        
        # Predefined aspect ratios and their string representations
        aspect_ratios = {
            "16:9": 16 / 9,
            "1:1": 1 / 1,
            "21:9": 21 / 9,
            "2:3": 2 / 3,
            "3:2": 3 / 2,
            "4:5": 4 / 5,
            "5:4": 5 / 4,
            "9:16": 9 / 16,
            "9:21": 9 / 21
        }
        
        # Find the closest aspect ratio
        closest_ratio = min(aspect_ratios, key=lambda k: abs(aspect_ratios[k] - aspect_ratio))
        
        return closest_ratio
    
    def generate_scenario_image(self, url_scenario, model_id, authorize_scenario, description, width, height, should_remove_background):
        payload_scenario = {
            "modelId": model_id,
            "qualityBoost": False,
            "hideResults": False,
            "intermediateImages": False,
            "prompt": description,
            "numInferenceSteps": 30,
            "numSamples": 1,
            "guidance": 7.5,
            "width": width,                   
            "height": height
        }
        headers_scenario = {
            "accept": "application/json",
            "content-type": "application/json",
            "Authorization": "Basic <<"+authorize_scenario+">>"
        }
        response = requests.post(url_scenario, json=payload_scenario, headers=headers_scenario)
        if response.status_code == 200:
            data = response.json()
            print(data)
            inference_id = data['inference']['id']
            
            img_id, img_url = self.get_sceneario_image_url(self.scenario_base_url, model_id, inference_id, headers_scenario)
            if img_id == None:
                return None
            else:
                if should_remove_background:
                    img_background_url = self.remove_background_image(self.scenario_base_url, headers_scenario, img_id)
                    return requests.get(img_background_url, headers=headers_scenario).content
                else:
                    return requests.get(img_url, headers=headers_scenario).content
        else:
            print(f'Error: {response.status_code}')
            return None

    def image_derivation(self) -> bytes:
            args = request.args
            prompt = request.json
            userID = int(prompt["userid"])
            username = prompt["username"]
            password = prompt["password"]
            provider = prompt["provider"]
            description = prompt["description"]
            exclude = prompt["exclude"]
            steps = int(prompt["steps"])
            width = int(prompt["width"])
            height = int(prompt["height"])
            image_sixtyfour = str(prompt["data"])
                
            print ("image_derivation::IMAGE[" + str(len(image_sixtyfour))+ "]::prompt=" + description)
            # image_data = base64.b64decode(image_sixtyfour)
            # print ("IMAGE LENGTH=" + str(len(image_data)))
            
            url_scenario = self.scenario_base_url + "/generate/img2img"
            model_id = self.scenario_model_character
            authorize_scenario = base64.b64encode(self.scenario_config.encode('ascii')).decode('ascii')
            payload_scenario = {
                "modelId": model_id,
                "qualityBoost": False,
                "hideResults": False,
                "prompt": description,
                "image": "data:image/png;base64,"+image_sixtyfour, # Your image dataURL here                 
                "numInferenceSteps": 30,
                "numSamples": 1,
                "guidance": 7.5,
                "width": width,                   
                "height": height
            }
            headers_scenario = {
                "accept": "application/json",
                "content-type": "application/json",
                "Authorization": "Basic <<"+authorize_scenario+">>"
            }
            response = requests.post(url_scenario, json=payload_scenario, headers=headers_scenario)
            if response.status_code == 200:
                data = response.json()
                print(data)
                inference_id = data['inference']['id']
                
                img_id, img_url = self.get_sceneario_image_url(self.scenario_base_url, model_id, inference_id, headers_scenario)
                if img_id == None:
                    return None
                else:
                    img_background_url = self.remove_background_image(self.scenario_base_url, headers_scenario, img_id)
                    return requests.get(img_background_url, headers=headers_scenario).content                     
            else:
                return None
                
    def align_text_audio(self):
        try:
            # Get audio file and transcript from the request
            userID = request.form.get("userid")
            username = request.form.get("username")
            password = request.form.get("password")
            transcript = request.form.get("transcript")
            language = request.form.get("language")
            audio_file = request.files.get("audio") 

            self.init_sql_functions(username)

            if self.enable_user_check and not self.sqlFunctions.login_user_id(userID, username, password, self.port_number):
                return "Error: No matching user and password"            

            # ### Convert OGG to MP3
            ogg_audio = AudioSegment.from_file(audio_file, format="ogg")
            ogg_audio = ogg_audio.set_channels(1)  # Convert to mono
            ogg_audio = ogg_audio.set_frame_rate(16000)  # Convert to 16kHz
            ogg_audio = ogg_audio.set_sample_width(2)  # 16-bit PCM            

            # ### Save the WAV file temporarily
            audio_path = './uploaded_audio'+str(userID)+'.wav'
            ogg_audio.export(audio_path, format="wav", parameters=["-acodec", "pcm_s16le"])  # Ensures 16-bit PCM

            # ### Perform forced alignment
            # print ("language=" + language)
            # print ("transcript=" + transcript)
            with open(audio_path, "rb") as audio_file:
                response = requests.post(
                    "http://127.0.0.1:6006/align",
                    files={"audio": audio_file},
                    data={"transcript": transcript, "language": language}
                )

            # Check the response
            if response.status_code == 200:
                alignment_data = response.json()
                # print("Alignment results:", alignment_data)
            else:
                print("Error:", response.json())            

            os.remove(audio_path)

            return alignment_data, 200

        except Exception as e:
            return jsonify({"error": str(e)}), 500
    
    def list_voices(self):
        try:
            # Get audio file and transcript from the request
            args = request.args
            prompt = request.json
            userID = int(prompt["userid"])
            username = prompt["username"]
            password = prompt["password"]
            language = prompt["language"]
            provider = int(prompt["provider"])

            # print("list_voices:: STEP 0::PROVIDER["+str(provider)+"]::LANGUAGE["+language+"]")

            self.init_sql_functions(username)

            if self.enable_user_check and not self.sqlFunctions.login_user_id(userID, username, password, self.port_number):
                return "Error: No matching user and password"            

            # //////// ELEVENLABS PROVIDER ////////
            if provider == 1: 
                headers = {"xi-api-key": self.apikey_elevenlabs}

                response = requests.get("https://api.elevenlabs.io/v1/voices", headers=headers)

                if response.status_code == 200:
                    voice_json = response.json()
                    voice_list = voice_json.get("voices", [])
                    simplified_list = []
                    for voice in voice_list:
                        if voice.get("fine_tuning") and voice.get("labels") and voice["fine_tuning"].get("language") == language:
                            simplified_voice = {
                                "display_name": voice["name"],
                                "id": voice["voice_id"],
                                "gender": voice["labels"].get("gender", "none"),
                                "description": voice.get("description", ""),
                                "audio": voice["preview_url"]
                            }
                            simplified_list.append(simplified_voice)
                    
                    simplified_json = json.dumps(simplified_list, indent=2)
                    
                    return simplified_json
                else:
                    return jsonify({"valid": False, "error": "Invalid API key"}), 401                

            # //////// OPENAI PROVIDER ////////                        
            elif provider == 2:                    
                return jsonify({"valid": False, "error": "Invalid API key"}), 401                
                
            # //////// LMNT PROVIDER ////////                        
            elif provider == 3:
                headers = {"X-API-Key": self.apikey_lmnt}

                response = requests.request("GET", "https://api.lmnt.com/v1/ai/voice/list", headers=headers)

                if response.status_code == 200:
                    voice_list = response.json()            
                    simplified_list = []
                    for voice in voice_list:
                        final_gender = "male"
                        if voice.get("gender", "") == "F":
                            final_gender = "female"
                        else:
                            final_gender = "male"
                        simplified_voice = {
                            "display_name": voice["name"],
                            "id": voice["id"],
                            "gender": final_gender,
                            "description": voice["description"],
                            "audio": ''
                        }
                        simplified_list.append(simplified_voice)

                    simplified_json = json.dumps(simplified_list, indent=2)
                
                    return simplified_json

                else:
                    return jsonify({"valid": False, "error": "Invalid API key"}), 401
                
            # //////// CARTESIA PROVIDER ////////                            
            elif provider == 4:
                headers = {
                    "Cartesia-Version": "2024-06-10",
                    "X-API-Key": self.apikey_cartesia
                }
                
                response = requests.request("GET", "https://api.cartesia.ai/voices/?limit=100", headers=headers)
                
                if response.status_code == 200:
                    voice_list = response.json()
                    simplified_list = []
                    for voice in voice_list:
                        if voice:
                            if voice.get("language") and voice.get("language") == language:
                                final_gender = "male"
                                if voice.get("gender", "") == "masculine":
                                    final_gender = "male"
                                else:
                                    final_gender = "female"                        
                                simplified_voice = {
                                    "display_name": voice["name"],
                                    "id": voice["id"],
                                    "gender": final_gender,
                                    "description": voice.get("description", ""),
                                    "audio": ""
                                }
                                simplified_list.append(simplified_voice)

                    simplified_json = json.dumps(simplified_list, indent=2)
                    
                    return simplified_json
                else:
                    return jsonify({"valid": False, "error": "Invalid API key"}), 401                
            
            # //////// SPEECHIFY PROVIDER ////////                            
            elif provider == 5:
                headers = { "Authorization": f"Bearer {self.apikey_speechify}" }
                
                response = requests.request("GET", "https://api.sws.speechify.com/v1/voices", headers=headers)            

                final_language = language + "-"
    
                if response.status_code == 200:
                    voice_list = response.json()            
                    simplified_list = []
                    for voice in voice_list:
                        if voice.get("models") and len(voice["models"]) > 0:
                            model = voice["models"][0]
                            if model.get("languages") and isinstance(model["languages"], list) and len(model["languages"]) > 0:
                                for lang_item in model["languages"]:
                                    if lang_item.get("locale").find(final_language) != -1:
                                        simplified_voice = {
                                            "display_name": voice["display_name"],
                                            "id": voice["id"],
                                            "gender": voice["gender"],
                                            "description": '',
                                            "audio": lang_item.get("preview_audio")
                                        }
                                        simplified_list.append(simplified_voice)

                    simplified_json = json.dumps(simplified_list, indent=2)
                
                    return simplified_json
                else:
                    return jsonify({"valid": False, "error": "Invalid API key"}), 401                
            
            # //////// PLAYHT PROVIDER ////////                            
            elif provider == 6:
                headers = {
                    "AUTHORIZATION": f"Bearer {self.apikey_playht_password}",
                    "X-USER-ID": self.apikey_playht_user
                }
                response = requests.request("GET", "https://api.play.ht/api/v2/voices", headers=headers)
            
                final_language = language + "-"
            
                print("list_voices::PLAYHT:: STEP 0::CODE["+str(response.status_code) +"]::LANGUAGE["+final_language+"]")
            
                if response.status_code == 200:
                    voice_list = response.json()            
                    simplified_list = []
                    for voice in voice_list:
                        if voice.get("language_code") and len(voice["language"]) > 0:
                            if voice.get("language_code").find(final_language) != -1:
                                simplified_voice = {
                                    "display_name": voice["name"],
                                    "id": voice["id"],
                                    "gender": voice["gender"],
                                    "description": '',
                                    "audio": voice["sample"]
                                }
                                simplified_list.append(simplified_voice)

                    simplified_json = json.dumps(simplified_list, indent=2)
                
                    return simplified_json
                else:
                    return jsonify({"valid": False, "error": "Invalid API key"}), 401                

                return jsonify({"valid": False, "error": "Invalid API key"}), 401                

        except Exception as e:
            return jsonify({"error": str(e)}), 500    
    
    def stop(self):
            return jsonify(status="ok")

    def status(self):
            return jsonify(status="ok")
        
    def start_webserver(self):
            self.app.run(host=self.host_address, port=self.port_number, threaded=False)
        

