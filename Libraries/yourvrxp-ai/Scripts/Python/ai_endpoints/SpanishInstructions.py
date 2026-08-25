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


# // FORMAT VISUAL IMAGE //
class ImageForScene(BaseModel):
    name: str = Field(description="Nombre de la imagen")
    scene: str = Field(description="Nombre de la escena")
    description: str = Field(description="Descripción de la imagen que representa la escena")

# // FORMAT SOUND FX //
class SoundFXForScene(BaseModel):
    name: str = Field(description="Nombre del efecto de sonido")
    paragraphid: int = Field(description="Número de identificación del parágrafo donde se reproduce el efecto de sonido")
    description: str = Field(description="Descripción breve de 6 palabras que describe el efecto de sonido asociado a un evento que sucede en el parágrafo")

# // FORMAT MUSIC LOOP //
class MusicLoopForScene(BaseModel):
    name: str = Field(description="Nombre de la música")
    scene: str = Field(description="Nombre de la escena")
    description: str = Field(description="Descripción breve de 12 palabras que describe el estilo del bucle musical asociado al ambiente de la escena")

# ========================
# == FORMAT TRANSLATION ==
class TranslateToken(BaseModel):
    originaltext: str = Field(description="El texto a traducir")
    translatedtext: str = Field(description="El texto traducido")
    
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
        self.databaseAlchemy = 'sqlite:///aibookeditordata_es.db'
        self.voicesLanguage = '/home/esteban/Workspace/Flask/wav_voices/es'  # Set this to your desired directory
        self.templateQuestion = """En idioma Español, la IA debe seguir las intrucciones y preguntas que recibe del humano.

                            Conversación actual:
                            {history}
                            Humano: {input}
                            Asistente IA:"""


        # /////////////////////////
        # // FORMAT VISUAL IMAGE //
        self.parserFormatImage = JsonOutputParser(pydantic_object=ImageForScene)
        self.promptFormatImage = PromptTemplate(
            template="En idioma Español, responde a la siguiente petición del usuario.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatImage.get_format_instructions()},
        )

        # /////////////////////
        # // FORMAT SOUND FX //
        self.parserFormatSoundFX = JsonOutputParser(pydantic_object=SoundFXForScene)
        self.promptFormatSoundFX = PromptTemplate(
            template="En idioma Español, responde a la siguiente petición del usuario.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatSoundFX.get_format_instructions()},
        )

        # ///////////////////////
        # // FORMAT MUSIC LOOP //
        self.parserFormatMusicLoop = JsonOutputParser(pydantic_object=MusicLoopForScene)
        self.promptFormatMusicLoop = PromptTemplate(
            template="En idioma Español, responde a la siguiente petición del usuario.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatMusicLoop.get_format_instructions()},
        )

        # ========================
        # == FORMAT TRANSLATION ==
        self.parserFormatTranslateToken = JsonOutputParser(pydantic_object=TranslateToken)
        self.promptFormatTranslateToken = PromptTemplate(
            template="La IA debe traducir el texto al idioma Español utilizando la información proporcionada por el humano.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatTranslateToken.get_format_instructions()},
        )

        # ++++++++++++++++++++
        # ++ TRANSLATE TEXT ++ 
        self.templateTranslation = """La IA debe traducir el texto contenido dentro del tag XML <textsource> al idioma Español.

                    Conversación actual:
                    {history}
                    <textsource> {input} </textsource>
                    Asistente IA:"""   
