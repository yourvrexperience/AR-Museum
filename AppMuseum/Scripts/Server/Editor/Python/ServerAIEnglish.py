from ai_endpoints.AILLMEndpoints import AILLMServer
from instructions.EnglishInstructions import InstructionsAI
import argparse
import os
from pydantic import BaseModel, Field
from typing import List
from flask_cors import CORS

instructions_ai = InstructionsAI()

PORT = int(os.getenv("AI_SERVER_PORT", "5001"))

ai_llm_server = AILLMServer(
    '0.0.0.0',
    PORT,
    instructions_ai.databaseAlchemy,
    instructions_ai.templateQuestion,
)

app = ai_llm_server.app 

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description="Start the AI LLM Server.")
    parser.add_argument('--port', type=int, default=PORT,
                        help='Port number for the dev server (default: 5001)')
    args = parser.parse_args()
    ai_llm_server.port_number = args.port
    ai_llm_server.start_webserver()
