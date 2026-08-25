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
    name: str = Field(description="Name des Bildes")
    scene: str = Field(description="Name der Szene")
    description: str = Field(description="Beschreibung des Bildes, das die Szene darstellt")

class SoundFXForScene(BaseModel):
    name: str = Field(description="Name des Soundeffekts")
    paragraphid: int = Field(description="Identifikationsnummer des Abschnitts, in dem der Soundeffekt abgespielt wird")
    description: str = Field(description="Kurze Beschreibung von 6 Wörtern, die einen Soundeffekt eines Ereignisses beschreibt, das im Abschnitt passiert")

class MusicLoopForScene(BaseModel):
    name: str = Field(description="Name der Musikschleife")
    scene: str = Field(description="Name der Szene")
    description: str = Field(description="Kurze Beschreibung von 12 Wörtern, die den Stil der Musikschleife im Zusammenhang mit der Stimmung der Szene beschreibt")

class TranslateToken(BaseModel):
    originaltext: str = Field(description="Der zu übersetzende Text")
    translatedtext: str = Field(description="Der übersetzte Text")

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
        self.databaseAlchemy = 'sqlite:///aibookeditordata_de.db'
        self.voicesLanguage = '/home/esteban/Workspace/Flask/wav_voices/de'  # Set this to your desired directory
        self.templateQuestion = """In der deutsch Sprache soll die KI den Anweisungen und Anfragen des Menschen folgen.
                        Aktuelles Gespräch:
                        {history}
                        Mensch: {input}
                        KI-Assistent:"""

        # /////////////////////////
        # // FORMAT VISUAL IMAGE //
        self.parserFormatImage = JsonOutputParser(pydantic_object=ImageForScene)
        self.promptFormatImage = PromptTemplate(
            template="In deutsch Sprache, beantworte die Anfrage des Nutzers.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatImage.get_format_instructions()},
        )

        # /////////////////////
        # // FORMAT SOUND FX //
        self.parserFormatSoundFX = JsonOutputParser(pydantic_object=SoundFXForScene)
        self.promptFormatSoundFX = PromptTemplate(
            template="In deutsch Sprache, beantworte die Anfrage des Nutzers.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatSoundFX.get_format_instructions()},
        )

        # ///////////////////////
        # // FORMAT MUSIC LOOP //
        self.parserFormatMusicLoop = JsonOutputParser(pydantic_object=MusicLoopForScene)
        self.promptFormatMusicLoop = PromptTemplate(
            template="In deutsch Sprache, beantworte die Anfrage des Nutzers.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatMusicLoop.get_format_instructions()},
        )

        # ========================
        # == FORMAT TRANSLATION ==
        self.parserFormatTranslateToken = JsonOutputParser(pydantic_object=TranslateToken)
        self.promptFormatTranslateToken = PromptTemplate(
            template="Die KI muss den Text unter Verwendung der vom Menschen bereitgestellten Informationen ins Deutsche übersetzen.\n{format_instructions}\n{query}\n",
            input_variables=["query"],
            partial_variables={"format_instructions": self.parserFormatTranslateToken.get_format_instructions()},
        )

        # ++++++++++++++++++++
        # ++ TRANSLATE TEXT ++ 
        self.templateTranslation = """Die KI muss den Text, der im XML-Tag <textsource> enthalten ist, ins Deutsche übersetzen.
        
                    Aktuelles Gespräch:
                    {history}
                    <textsource> {input} </textsource>
                    KI-Assistent:"""   
