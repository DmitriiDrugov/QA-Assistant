from pydantic import BaseModel, EmailStr
from typing import Optional
from datetime import datetime


# ── Auth ─────────────────────────────────────────────────────────────────────
class UserCreate(BaseModel):
    email: EmailStr
    username: str
    password: str

class UserOut(BaseModel):
    id: str
    email: str
    username: str
    created_at: datetime
    class Config: orm_mode = True

class Token(BaseModel):
    access_token: str
    token_type: str


# ── Conversations ─────────────────────────────────────────────────────────────
class ConversationCreate(BaseModel):
    title: Optional[str] = None

class ConversationOut(BaseModel):
    id: str
    title: str
    created_at: datetime
    updated_at: datetime
    class Config: orm_mode = True


# ── Messages ──────────────────────────────────────────────────────────────────
class MessageCreate(BaseModel):
    content: str

class MessageOut(BaseModel):
    id: str
    role: str
    content: str
    created_at: datetime
    class Config: orm_mode = True


# ── AI Settings ───────────────────────────────────────────────────────────────
class AISettingsUpdate(BaseModel):
    model_endpoint: Optional[str] = None
    temperature: Optional[float] = None
    max_tokens: Optional[int] = None
    system_prompt: Optional[str] = None

class AISettingsOut(BaseModel):
    model_endpoint: str
    temperature: float
    max_tokens: int
    system_prompt: str
    class Config: orm_mode = True
