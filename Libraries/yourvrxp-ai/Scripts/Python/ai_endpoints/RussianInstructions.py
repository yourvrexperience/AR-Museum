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

class ImageForScene(BaseModel):
    name: str = Field(description="Название изображения")
    scene: str = Field(description="Название сцены")
    description: str = Field(description="Описание изображения, которое представляет сцену")

class SoundFXForScene(BaseModel):
    name: str = Field(description="Название звукового эффекта")
    paragraphid: int = Field(description="Идентификационный номер абзаца, в котором звучит звуковой эффект")
    description: str = Field(description="Краткое описание в 6 слов, которое описывает звуковой эффект события, происходящего в абзаце")

class MusicLoopForScene(BaseModel):
    name: str = Field(description="Название музыкального фона")
    scene: str = Field(description="Название сцены")
    description: str = Field(description="Краткое описание в 12 слов, которое описывает стиль музыкального фона, соответствующего настроению этой сцены")

class TranslateToken(BaseModel):
    originaltext: str = Field(description="Текст для перевода")
    translatedtext: str = Field(description="Переведенный текст")

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
        self.databaseAlchemy = 'sqlite:///aibookeditordata_ru.db'
        self.voicesLanguage = '/home/esteban/Workspace/Flask/wav_voices/ru'  # Set this to your desired directory
        self.templateQuestion = """По-русски искусственный интеллект должен следовать инструкциям и запросам человека.

                        Текущий разговор:
                        {history}
                        Человек: {input}
                        ИИ-ассистент:"""


        # /////////////////////////
        # // FORMAT VISUAL IMAGE //
        self.parserFormatImage = JsonOutputParser(pydantic_object=ImageForScene)
        self.promptFormatImage = PromptTemplate(
            template="Ответьте на запрос пользователя на русском языке.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatImage.get_format_instructions()},
        )

        # /////////////////////
        # // FORMAT SOUND FX //
        self.parserFormatSoundFX = JsonOutputParser(pydantic_object=SoundFXForScene)
        self.promptFormatSoundFX = PromptTemplate(
            template="Ответьте на запрос пользователя на русском языке.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatSoundFX.get_format_instructions()},
        )

        # ///////////////////////
        # // FORMAT MUSIC LOOP //
        self.parserFormatMusicLoop = JsonOutputParser(pydantic_object=MusicLoopForScene)
        self.promptFormatMusicLoop = PromptTemplate(
            template="Ответьте на запрос пользователя на русском языке.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatMusicLoop.get_format_instructions()},
        )

        # ========================
        # == FORMAT TRANSLATION ==
        self.parserFormatTranslateToken = JsonOutputParser(pydantic_object=TranslateToken)
        self.promptFormatTranslateToken = PromptTemplate(
            template="Искусственный интеллект должен перевести текст на русский язык, используя информацию, предоставленную человеком.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatTranslateToken.get_format_instructions()},
        )

        # ++++++++++++++++++++
        # ++ TRANSLATE TEXT ++ 
        self.templateTranslation = """ИИ должен перевести текст, содержащийся внутри тега XML <textsource>, на русский язык.

                    Текущий разговор:
                    {history}
                    <textsource> {input} </textsource>
                    ИИ-ассистент:"""   
