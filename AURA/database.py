from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker
import os

# SQLite for dev — swap with PostgreSQL for production:
# DATABASE_URL = "postgresql://user:password@localhost/aura_db"
DATABASE_URL = os.getenv("DATABASE_URL", "sqlite:///./aura.db")

engine = create_engine(
    DATABASE_URL,
    connect_args={"check_same_thread": False} if DATABASE_URL.startswith("sqlite") else {}
)

SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()
