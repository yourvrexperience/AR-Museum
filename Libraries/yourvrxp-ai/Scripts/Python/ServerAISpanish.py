from ai_endpoints.AILLMEndpoints import AILLMServer
from ai_endpoints.SpanishInstructions import InstructionsAI
import argparse

# We need JSON prompts for each language
from pydantic import BaseModel, Field
from typing import List
from langchain.schema.messages import HumanMessage, AIMessage
from langchain.chains import ConversationChain
from langchain_core.output_parsers import JsonOutputParser
from langchain_core.prompts import PromptTemplate
from langchain.memory import ConversationBufferMemory 
from langchain_core.prompts.prompt import PromptTemplate
from langchain.memory import ConversationSummaryMemory
from langchain.prompts import (
    ChatPromptTemplate,
    HumanMessagePromptTemplate,
    MessagesPlaceholder,
    SystemMessagePromptTemplate,
)
    
instructions_ai = InstructionsAI()    

# ************************************
# ************************************
# OLLAMA SERVER ENDPOINTS
# ************************************
# ************************************

if __name__ == '__main__':
    # Create the argument parser
    parser = argparse.ArgumentParser(description="Start the AI LLM Server.")
    parser.add_argument(
        '--port', type=int, default=5002, help='Port number for the server (default: 5000)'
    )
    
    # Parse the arguments
    args = parser.parse_args()
    
    ai_llm_server = AILLMServer('0.0.0.0',
                            args.port,
                            instructions_ai.databaseAlchemy, 
                            instructions_ai.voicesLanguage, 
                            instructions_ai.urlSpeechGeneration,
                            instructions_ai.urlImageGeneration,
                            instructions_ai.urlFluxImageGeneration,
                            instructions_ai.templateQuestion,
                            instructions_ai.promptFormatImage,
                            instructions_ai.parserFormatImage,
                            instructions_ai.promptFormatSoundFX,
                            instructions_ai.parserFormatSoundFX,
                            instructions_ai.promptFormatMusicLoop,
                            instructions_ai.parserFormatMusicLoop,                            
                            instructions_ai.promptFormatTranslateToken,
                            instructions_ai.parserFormatTranslateToken,                                    
                            instructions_ai.promptParagraphElevenLabsVoiceSettings,
                            instructions_ai.parserParagraphElevenLabsVoiceSettings,
                            instructions_ai.templateTranslation
                            )
    ai_llm_server.start_webserver()
