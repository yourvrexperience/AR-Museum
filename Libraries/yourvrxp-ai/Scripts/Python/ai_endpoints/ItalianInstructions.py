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
    name: str = Field(description="Nome dell'immagine")
    scene: str = Field(description="Nome della scena")
    description: str = Field(description="Descrizione dell'immagine che rappresenta la scena")

class SoundFXForScene(BaseModel):
    name: str = Field(description="Nome dell'effetto sonoro")
    paragraphid: int = Field(description="Numero identificativo del paragrafo in cui viene riprodotto l'effetto sonoro")
    description: str = Field(description="Breve descrizione di 6 parole che descrive un effetto sonoro di un evento che accade nel paragrafo")

class MusicLoopForScene(BaseModel):
    name: str = Field(description="Nome del loop musicale")
    scene: str = Field(description="Nome della scena")
    description: str = Field(description="Breve descrizione di 12 parole che descrive lo stile del loop musicale legato all'atmosfera di quella scena")

class TranslateToken(BaseModel):
    originaltext: str = Field(description="Il testo da tradurre")
    translatedtext: str = Field(description="Il testo tradotto")

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
        self.databaseAlchemy = 'sqlite:///aibookeditordata_it.db'
        self.voicesLanguage = '/home/esteban/Workspace/Flask/wav_voices/it'  # Set this to your desired directory
        self.templateQuestion = """Nella lingua italiana l’AI deve seguire le istruzioni e le richieste impartite dall’utente umano.

                        Conversazione corrente:
                        {history}
                        Utente: {input}
                        Assistente IA:"""


        # /////////////////////////
        # // FORMAT VISUAL IMAGE //
        self.parserFormatImage = JsonOutputParser(pydantic_object=ImageForScene)
        self.promptFormatImage = PromptTemplate(
            template="In italiano, rispondere alla richiesta dell'utente.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatImage.get_format_instructions()},
        )

        # /////////////////////
        # // FORMAT SOUND FX //
        self.parserFormatSoundFX = JsonOutputParser(pydantic_object=SoundFXForScene)
        self.promptFormatSoundFX = PromptTemplate(
            template="In italiano, rispondere alla richiesta dell'utente.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatSoundFX.get_format_instructions()},
        )

        # ///////////////////////
        # // FORMAT MUSIC LOOP //
        self.parserFormatMusicLoop = JsonOutputParser(pydantic_object=MusicLoopForScene)
        self.promptFormatMusicLoop = PromptTemplate(
            template="In italiano, rispondere alla richiesta dell'utente.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatMusicLoop.get_format_instructions()},
        )

        # ========================
        # == FORMAT TRANSLATION ==
        self.parserFormatTranslateToken = JsonOutputParser(pydantic_object=TranslateToken)
        self.promptFormatTranslateToken = PromptTemplate(
            template="L'IA deve tradurre il testo in italiano utilizzando le informazioni fornite dagli esseri umani.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatTranslateToken.get_format_instructions()},
        )

        # ++++++++++++++++++++
        # ++ TRANSLATE TEXT ++ 
        self.templateTranslation = """L'IA deve tradurre il testo contenuto nel tag XML <textsource> in italiano.

                    Conversazione corrente:
                    {history}
                    <textsource> {input} </textsource>
                    Assistente IA:"""   
