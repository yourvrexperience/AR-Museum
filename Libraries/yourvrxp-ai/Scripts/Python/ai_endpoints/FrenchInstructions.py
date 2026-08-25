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
    name: str = Field(description="Nom de l'image")
    scene: str = Field(description="Nom de la scène")
    description: str = Field(description="Description de l'image qui représente la scène")

class SoundFXForScene(BaseModel):
    name: str = Field(description="Nom de l'effet sonore")
    paragraphid: int = Field(description="Numéro d'identification du paragraphe où l'effet sonore est joué")
    description: str = Field(description="Courte description de 6 mots qui décrit un effet sonore d'un événement qui se produit dans le paragraphe")

class MusicLoopForScene(BaseModel):
    name: str = Field(description="Nom de la boucle musicale")
    scene: str = Field(description="Nom de la scène")
    description: str = Field(description="Courte description de 12 mots qui décrit le style de la boucle musicale liée à l'humeur de la scène")

class TranslateToken(BaseModel):
    originaltext: str = Field(description="Texte à traduire")
    translatedtext: str = Field(description="Texte traduit")

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
        self.databaseAlchemy = 'sqlite:///aibookeditordata_fr.db'
        self.voicesLanguage = '/home/esteban/Workspace/Flask/wav_voices/fr'  # Set this to your desired directory
        self.templateQuestion = """En langue française, l’IA doit suivre les instructions et demandes fournies par l’utilisateur.

                      Conversation actuelle :
                      {history}
                      Utilisateur : {input}
                      Assistant IA :"""

        # /////////////////////////
        # // FORMAT VISUAL IMAGE //
        self.parserFormatImage = JsonOutputParser(pydantic_object=ImageForScene)
        self.promptFormatImage = PromptTemplate(
            template="En langue française, répondre à la question de l'utilisateur.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatImage.get_format_instructions()},
        )

        # /////////////////////
        # // FORMAT SOUND FX //
        self.parserFormatSoundFX = JsonOutputParser(pydantic_object=SoundFXForScene)
        self.promptFormatSoundFX = PromptTemplate(
            template="En langue française, répondre à la question de l'utilisateur.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatSoundFX.get_format_instructions()},
        )

        # ///////////////////////
        # // FORMAT MUSIC LOOP //
        self.parserFormatMusicLoop = JsonOutputParser(pydantic_object=MusicLoopForScene)
        self.promptFormatMusicLoop = PromptTemplate(
            template="En langue française, répondre à la question de l'utilisateur.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatMusicLoop.get_format_instructions()},
        )

        # ========================
        # == FORMAT TRANSLATION ==
        self.parserFormatTranslateToken = JsonOutputParser(pydantic_object=TranslateToken)
        self.promptFormatTranslateToken = PromptTemplate(
            template="L’IA doit traduire le texte en français en utilisant les informations fournies par les humains.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatTranslateToken.get_format_instructions()},
        )

        # ++++++++++++++++++++
        # ++ TRANSLATE TEXT ++ 
        self.templateTranslation = """L'IA doit traduire le texte contenu dans la balise XML <textsource> en français.
        
                    Conversation en cours :
                    {history}
                    <textsource> {input} </textsource>
                    Assistant IA:"""   
