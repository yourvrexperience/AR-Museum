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

# ++++++++++++++++++++++
# ++++++++++++++++++++++
# STORY
# ++++++++++++++++++++++
# ++++++++++++++++++++++

# /////////////////////////
# // FORMAT VISUAL IMAGE //
class ImageForScene(BaseModel):
    name: str = Field(description="Name of the image")
    scene: str = Field(description="Name of the scene")
    description: str = Field(description="Description of the image that represents the scene")

# /////////////////////
# // FORMAT SOUND FX //
class SoundFXForScene(BaseModel):
    name: str = Field(description="Name of the sound effect")
    paragraphid: int = Field(description="Identification number of paragraph where the sound effect plays")
    description: str = Field(description="Short description of 6 words that describes a sound effect of an event that happens in the paragraph")

# /////////////////////
# // FORMAT MUSIC LOOP //
class MusicLoopForScene(BaseModel):
    name: str = Field(description="Name of the music loop")
    scene: str = Field(description="Name of the scene")
    description: str = Field(description="Short description of 12 words that describes the style of the music loop linked to the mood of that scene")

# ========================
# == FORMAT TRANSLATION ==
class TranslateToken(BaseModel):
    originaltext: str = Field(description="The text to translate")
    translatedtext: str = Field(description="The translated text")
    
# **************************************************************
# **************************************************************
# **************************************************************
# INSTRUCTIONS AI
# **************************************************************
# **************************************************************
# **************************************************************

class InstructionsAI:
    def __init__(self):
        self.urlSpeechGeneration = "http://0.0.0.0:6000"    
        self.urlImageGeneration = "http://0.0.0.0:7860"             
        self.urlFluxImageGeneration = "http://0.0.0.0:7869"
        # self.urlFluxImageGeneration = "https://f26be2c194c343be51.gradio.live"         
        self.databaseAlchemy = 'sqlite:///aibookeditordata_en.db'
        self.voicesLanguage = '/home/esteban/Workspace/Flask/wav_voices/en'  # Set this to your desired directory
        self.templateQuestion = """In English language, the AI should follow the instructions and requests provided by the human.

                        Current conversation:
                        {history}
                        Human: {input}
                        AI Assistant:"""

        # /////////////////////////
        # // FORMAT VISUAL IMAGE //
        self.parserFormatImage = JsonOutputParser(pydantic_object=ImageForScene)
        self.promptFormatImage = PromptTemplate(
            template="In English language, answer the user query.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatImage.get_format_instructions()},
        )

        # /////////////////////
        # // FORMAT SOUND FX //
        self.parserFormatSoundFX = JsonOutputParser(pydantic_object=SoundFXForScene)
        self.promptFormatSoundFX = PromptTemplate(
            template="In English language, answer the user query.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatSoundFX.get_format_instructions()},
        )

        # ///////////////////////
        # // FORMAT MUSIC LOOP //
        self.parserFormatMusicLoop = JsonOutputParser(pydantic_object=MusicLoopForScene)
        self.promptFormatMusicLoop = PromptTemplate(
            template="In English language, answer the user query.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatMusicLoop.get_format_instructions()},
        )

        # ========================
        # == FORMAT TRANSLATION ==
        self.parserFormatTranslateToken = JsonOutputParser(pydantic_object=TranslateToken)
        self.promptFormatTranslateToken = PromptTemplate(
            template="The AI should translate the text to English language using the information provided by the user.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatTranslateToken.get_format_instructions()},
        )

        # ++++++++++++++++++++
        # ++ TRANSLATE TEXT ++ 
        self.templateTranslation = """The AI must translate the text contained within the XML tag <textsource> into English.

                    Current conversation:
                    {history}
                    <textsource> {input} </textsource>
                    AI Assistant:"""
