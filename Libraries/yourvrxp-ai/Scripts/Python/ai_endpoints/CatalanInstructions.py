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


# // FORMAT VISUAL IMATGE //
class ImageForScene(BaseModel):
    name: str = Field(description="Nom de la imatge")
    scene: str = Field(description="Nom de l'escena")
    description: str = Field(description="Descripció de la imatge que representa l'escena")

# // FORMAT SO FX //
class SoundFXForScene(BaseModel):
    name: str = Field(description="Nom de l'efecte de so")
    paragraphid: int = Field(description="Número d'identificació del paràgraf on es reprodueix l'efecte de so")
    description: str = Field(description="Descripció breu de 6 paraules que descriu l'efecte de so associat a un esdeveniment que succeeix al paràgraf")

# // FORMAT BUCLE MUSICAL //
class MusicLoopForScene(BaseModel):
    name: str = Field(description="Nom de la música")
    scene: str = Field(description="Nom de l'escena")
    description: str = Field(description="Descripció breu de 12 paraules que descriu l'estil del bucle musical associat a l'ambient de l'escena")
    
# == FORMAT TRADUCCIÓ ==
class TranslateToken(BaseModel):
    originaltext: str = Field(description="El text a traduir")
    translatedtext: str = Field(description="El text traduït")
    
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
        self.databaseAlchemy = 'sqlite:///aibookeditordata_ca.db'
        self.voicesLanguage = '/home/esteban/Workspace/Flask/wav_voices/ca'  # Ajusta això al directori que desitges
        self.templateQuestion = """En idioma Català, la IA ha de seguir les instruccions i preguntes que rep de l'humà.

                            Conversa actual:
                            {history}
                            Humà: {input}
                            Assistent IA:"""


        # /////////////////////////
        # // FORMAT VISUAL IMAGE //
        self.parserFormatImage = JsonOutputParser(pydantic_object=ImageForScene)
        self.promptFormatImage = PromptTemplate(
            template="En idioma Català, respon a la següent petició de l'usuari.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatImage.get_format_instructions()},
        )

        # /////////////////////
        # // FORMAT SOUND FX //
        self.parserFormatSoundFX = JsonOutputParser(pydantic_object=SoundFXForScene)
        self.promptFormatSoundFX = PromptTemplate(
            template="En idioma Català, respon a la següent petició de l'usuari.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatSoundFX.get_format_instructions()},
        )

        # ///////////////////////
        # // FORMAT MUSIC LOOP //
        self.parserFormatMusicLoop = JsonOutputParser(pydantic_object=MusicLoopForScene)
        self.promptFormatMusicLoop = PromptTemplate(
            template="En idioma Català, respon a la següent petició de l'usuari.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatMusicLoop.get_format_instructions()},
        )

        # ========================
        # == FORMAT TRADUCCIÓ ==
        self.parserFormatTranslateToken = JsonOutputParser(pydantic_object=TranslateToken)
        self.promptFormatTranslateToken = PromptTemplate(
            template="La IA ha de traduir el text a l'idioma Català utilitzant la informació proporcionada per l'humà.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatTranslateToken.get_format_instructions()},
        )

        # ++++++++++++++++++++
        # ++ TRADUIR TEXT ++ 
        self.templateTranslation = """La IA ha de traduir el text contingut dins de l'etiqueta XML <textsource> a l'idioma català.

                        Conversa actual:
                        {history}
                        <textsource> {input} </textsource>
                        Assistent IA:"""

