from fastapi import FastAPI, Depends, HTTPException, status
from fastapi.middleware.cors import CORSMiddleware
from fastapi.security import OAuth2PasswordBearer, OAuth2PasswordRequestForm
from sqlalchemy.orm import Session
from datetime import datetime, timedelta
from jose import JWTError, jwt
from passlib.context import CryptContext
from pydantic import BaseModel
from typing import Optional, List
import uuid

from database import get_db, engine
import models
import schemas

# Create all tables
models.Base.metadata.create_all(bind=engine)

app = FastAPI(title="AURA Chat API", version="1.0.0")

# ── CORS (allow your frontend origin) ──────────────────────────────────────
app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:3000", "http://127.0.0.1:5500"],  # Add your frontend URL
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# ── Auth config ─────────────────────────────────────────────────────────────
SECRET_KEY = "your-secret-key-change-this-in-production"  # Use env var in prod!
ALGORITHM = "HS256"
ACCESS_TOKEN_EXPIRE_MINUTES = 60 * 24  # 24 hours

pwd_context = CryptContext(schemes=["bcrypt"], deprecated="auto")
oauth2_scheme = OAuth2PasswordBearer(tokenUrl="/auth/login")


# ── Auth helpers ─────────────────────────────────────────────────────────────
def hash_password(password: str) -> str:
    return pwd_context.hash(password)

def verify_password(plain: str, hashed: str) -> bool:
    return pwd_context.verify(plain, hashed)

def create_access_token(data: dict, expires_delta: Optional[timedelta] = None):
    to_encode = data.copy()
    expire = datetime.utcnow() + (expires_delta or timedelta(minutes=15))
    to_encode.update({"exp": expire})
    return jwt.encode(to_encode, SECRET_KEY, algorithm=ALGORITHM)

def get_current_user(token: str = Depends(oauth2_scheme), db: Session = Depends(get_db)):
    credentials_exception = HTTPException(
        status_code=status.HTTP_401_UNAUTHORIZED,
        detail="Could not validate credentials",
        headers={"WWW-Authenticate": "Bearer"},
    )
    try:
        payload = jwt.decode(token, SECRET_KEY, algorithms=[ALGORITHM])
        user_id: str = payload.get("sub")
        if user_id is None:
            raise credentials_exception
    except JWTError:
        raise credentials_exception

    user = db.query(models.User).filter(models.User.id == user_id).first()
    if user is None:
        raise credentials_exception
    return user


# ── AUTH ROUTES ──────────────────────────────────────────────────────────────
@app.post("/auth/register", response_model=schemas.UserOut)
def register(user_in: schemas.UserCreate, db: Session = Depends(get_db)):
    existing = db.query(models.User).filter(models.User.email == user_in.email).first()
    if existing:
        raise HTTPException(status_code=400, detail="Email already registered")

    user = models.User(
        id=str(uuid.uuid4()),
        email=user_in.email,
        username=user_in.username,
        hashed_password=hash_password(user_in.password),
    )
    db.add(user)
    db.commit()
    db.refresh(user)
    return user

@app.post("/auth/login", response_model=schemas.Token)
def login(form: OAuth2PasswordRequestForm = Depends(), db: Session = Depends(get_db)):
    user = db.query(models.User).filter(models.User.email == form.username).first()
    if not user or not verify_password(form.password, user.hashed_password):
        raise HTTPException(status_code=401, detail="Incorrect email or password")

    token = create_access_token(
        data={"sub": user.id},
        expires_delta=timedelta(minutes=ACCESS_TOKEN_EXPIRE_MINUTES)
    )
    return {"access_token": token, "token_type": "bearer"}

@app.get("/auth/me", response_model=schemas.UserOut)
def me(current_user: models.User = Depends(get_current_user)):
    return current_user


# ── CONVERSATION ROUTES ───────────────────────────────────────────────────────
@app.get("/conversations", response_model=List[schemas.ConversationOut])
def list_conversations(
    current_user: models.User = Depends(get_current_user),
    db: Session = Depends(get_db)
):
    return db.query(models.Conversation)\
             .filter(models.Conversation.user_id == current_user.id)\
             .order_by(models.Conversation.updated_at.desc())\
             .all()

@app.post("/conversations", response_model=schemas.ConversationOut)
def create_conversation(
    conv_in: schemas.ConversationCreate,
    current_user: models.User = Depends(get_current_user),
    db: Session = Depends(get_db)
):
    conv = models.Conversation(
        id=str(uuid.uuid4()),
        user_id=current_user.id,
        title=conv_in.title or "New conversation",
    )
    db.add(conv)
    db.commit()
    db.refresh(conv)
    return conv

@app.delete("/conversations/{conv_id}")
def delete_conversation(
    conv_id: str,
    current_user: models.User = Depends(get_current_user),
    db: Session = Depends(get_db)
):
    conv = db.query(models.Conversation)\
             .filter(models.Conversation.id == conv_id,
                     models.Conversation.user_id == current_user.id).first()
    if not conv:
        raise HTTPException(status_code=404, detail="Conversation not found")
    db.delete(conv)
    db.commit()
    return {"ok": True}


# ── MESSAGE / CHAT ROUTES ─────────────────────────────────────────────────────
@app.get("/conversations/{conv_id}/messages", response_model=List[schemas.MessageOut])
def get_messages(
    conv_id: str,
    current_user: models.User = Depends(get_current_user),
    db: Session = Depends(get_db)
):
    conv = db.query(models.Conversation)\
             .filter(models.Conversation.id == conv_id,
                     models.Conversation.user_id == current_user.id).first()
    if not conv:
        raise HTTPException(status_code=404, detail="Conversation not found")

    return db.query(models.Message)\
             .filter(models.Message.conversation_id == conv_id)\
             .order_by(models.Message.created_at.asc())\
             .all()

@app.post("/conversations/{conv_id}/chat", response_model=schemas.MessageOut)
def chat(
    conv_id: str,
    msg_in: schemas.MessageCreate,
    current_user: models.User = Depends(get_current_user),
    db: Session = Depends(get_db)
):
    # Verify conversation belongs to user
    conv = db.query(models.Conversation)\
             .filter(models.Conversation.id == conv_id,
                     models.Conversation.user_id == current_user.id).first()
    if not conv:
        raise HTTPException(status_code=404, detail="Conversation not found")

    # Get AI model settings for this user
    ai_settings = db.query(models.AIModelSettings)\
                    .filter(models.AIModelSettings.user_id == current_user.id)\
                    .first()

    # Save user message
    user_msg = models.Message(
        id=str(uuid.uuid4()),
        conversation_id=conv_id,
        role="user",
        content=msg_in.content,
    )
    db.add(user_msg)

    # ── Call YOUR custom AI model ────────────────────────────────────────────
    # Get conversation history for context
    history = db.query(models.Message)\
                .filter(models.Message.conversation_id == conv_id)\
                .order_by(models.Message.created_at.asc())\
                .all()

    ai_reply = call_your_ai_model(
        user_message=msg_in.content,
        history=history,
        settings=ai_settings
    )
    # ─────────────────────────────────────────────────────────────────────────

    # Save AI reply
    ai_msg = models.Message(
        id=str(uuid.uuid4()),
        conversation_id=conv_id,
        role="assistant",
        content=ai_reply,
    )
    db.add(ai_msg)

    # Update conversation timestamp & auto-title
    conv.updated_at = datetime.utcnow()
    if conv.title == "New conversation" and len(history) == 0:
        conv.title = msg_in.content[:50] + ("..." if len(msg_in.content) > 50 else "")

    db.commit()
    db.refresh(ai_msg)
    return ai_msg


# ── AI MODEL SETTINGS ROUTES ──────────────────────────────────────────────────
@app.get("/settings/ai", response_model=schemas.AISettingsOut)
def get_ai_settings(
    current_user: models.User = Depends(get_current_user),
    db: Session = Depends(get_db)
):
    settings = db.query(models.AIModelSettings)\
                 .filter(models.AIModelSettings.user_id == current_user.id).first()
    if not settings:
        # Return defaults
        return schemas.AISettingsOut(
            model_endpoint="http://localhost:8001/predict",
            temperature=0.7,
            max_tokens=1024,
            system_prompt="You are a helpful AI assistant.",
        )
    return settings

@app.put("/settings/ai", response_model=schemas.AISettingsOut)
def update_ai_settings(
    settings_in: schemas.AISettingsUpdate,
    current_user: models.User = Depends(get_current_user),
    db: Session = Depends(get_db)
):
    settings = db.query(models.AIModelSettings)\
                 .filter(models.AIModelSettings.user_id == current_user.id).first()
    if not settings:
        settings = models.AIModelSettings(
            id=str(uuid.uuid4()),
            user_id=current_user.id,
        )
        db.add(settings)

    for field, value in settings_in.dict(exclude_unset=True).items():
        setattr(settings, field, value)

    db.commit()
    db.refresh(settings)
    return settings


# ── YOUR CUSTOM AI MODEL INTEGRATION ─────────────────────────────────────────
def call_your_ai_model(
    user_message: str,
    history: list,
    settings: Optional[models.AIModelSettings]
) -> str:
    """
    Replace this function with your actual AI model call.
    
    Options:
    - HTTP request to your model server (e.g. FastAPI, Flask, TorchServe)
    - Direct import if model runs in same process
    - gRPC call to a remote model service
    """
    import httpx

    endpoint = settings.model_endpoint if settings else "http://localhost:8001/predict"
    temperature = settings.temperature if settings else 0.7
    max_tokens = settings.max_tokens if settings else 1024
    system_prompt = settings.system_prompt if settings else "You are a helpful assistant."

    # Format history for your model
    formatted_history = [
        {"role": msg.role, "content": msg.content}
        for msg in history[-20:]  # Last 20 messages for context window
    ]

    payload = {
        "system": system_prompt,
        "messages": formatted_history + [{"role": "user", "content": user_message}],
        "temperature": temperature,
        "max_tokens": max_tokens,
    }

    try:
        response = httpx.post(endpoint, json=payload, timeout=30.0)
        response.raise_for_status()
        data = response.json()
        # Adjust key based on what your model API returns:
        return data.get("reply") or data.get("text") or data.get("output") or "No response."
    except Exception as e:
        raise HTTPException(status_code=502, detail=f"AI model error: {str(e)}")
