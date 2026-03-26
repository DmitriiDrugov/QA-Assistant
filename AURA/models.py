from sqlalchemy import Column, String, Text, Float, Integer, DateTime, ForeignKey
from sqlalchemy.orm import relationship
from sqlalchemy.ext.declarative import declarative_base
from datetime import datetime

Base = declarative_base()


class User(Base):
    __tablename__ = "users"

    id               = Column(String, primary_key=True, index=True)
    email            = Column(String, unique=True, index=True, nullable=False)
    username         = Column(String, nullable=False)
    hashed_password  = Column(String, nullable=False)
    created_at       = Column(DateTime, default=datetime.utcnow)

    conversations    = relationship("Conversation", back_populates="user", cascade="all, delete")
    ai_settings      = relationship("AIModelSettings", back_populates="user", uselist=False, cascade="all, delete")


class Conversation(Base):
    __tablename__ = "conversations"

    id         = Column(String, primary_key=True, index=True)
    user_id    = Column(String, ForeignKey("users.id"), nullable=False)
    title      = Column(String, default="New conversation")
    created_at = Column(DateTime, default=datetime.utcnow)
    updated_at = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)

    user       = relationship("User", back_populates="conversations")
    messages   = relationship("Message", back_populates="conversation", cascade="all, delete")


class Message(Base):
    __tablename__ = "messages"

    id              = Column(String, primary_key=True, index=True)
    conversation_id = Column(String, ForeignKey("conversations.id"), nullable=False)
    role            = Column(String, nullable=False)   # "user" | "assistant"
    content         = Column(Text, nullable=False)
    created_at      = Column(DateTime, default=datetime.utcnow)

    conversation    = relationship("Conversation", back_populates="messages")


class AIModelSettings(Base):
    __tablename__ = "ai_model_settings"

    id             = Column(String, primary_key=True, index=True)
    user_id        = Column(String, ForeignKey("users.id"), unique=True, nullable=False)
    model_endpoint = Column(String, default="http://localhost:8001/predict")
    temperature    = Column(Float, default=0.7)
    max_tokens     = Column(Integer, default=1024)
    system_prompt  = Column(Text, default="You are a helpful AI assistant.")
    updated_at     = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)

    user           = relationship("User", back_populates="ai_settings")
