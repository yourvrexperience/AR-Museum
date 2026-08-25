from pydantic import BaseModel, Field
from typing import List

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
        self.databaseAlchemy = 'sqlite:///yourowndatabase_en.db'
        self.templateQuestion = """In English language, the AI should follow the instructions and requests provided by the human.

                        Current conversation:
                        {history}
                        Human: {input}
                        AI Assistant:"""
