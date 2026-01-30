# AUMOVIO IT Support Q&A Application
Intern Assignment AI-Powered Q&A (C# .NET)



## 1. Introduction / Overview

This project is a simple AI-powered Question & Answer application implemented as a C# console application using .NET.

The goal of the application is to assist users with IT-related questions by searching a local knowledge base and, if available, using a Large Language Model (LLM) API to generate a helpful and relevant answer.

The solution focuses on simplicity, readability, and correct implementation of core concepts, which aligns with the expectations for an intern-level assignment.


## 2. How to Run the Application

## Prerequisites
- .NET 6 or later (tested with .NET 8)
- Visual Studio
- Internet connection (only required when using the AI API)

### Steps
1. Open the project in Visual Studio.
2. Ensure the file **`knowledge_base.txt`** is located in the same directory as **`Program.cs`**.( put " copy if newer")
3. In the code, replace the API key placeholder with a valid OpenAI API key or configure it using an environment variable.
4. Run the application using **Ctrl + F5**.
5. Enter an IT-related question in the console when prompted.
6. The application will display the selected knowledge base section and the AI-generated answer.



## 3. Architecture and Design Choices

The application is implemented as a **single C# console application** using one main file (`Program.cs`).

### Application Flow
1. Load the knowledge base from a local text file.
2. Split the content into smaller text chunks.
3. Accept a question from the user via the console.
4. Identify the most relevant chunk using keyword matching.
5. Send the selected chunk and the user question to the AI API.
6. Display the generated answer in the console.

### Design Decisions
- **Single-file structure:** Chosen to keep the code easy to read and understand.
- **Console application:** Meets the assignment requirements without unnecessary complexity.
- **Simple methods:** Each main task (chunking, searching, API call) is handled by a separate method to improve clarity and maintainability.



## 4. Knowledge Base Handling

The knowledge base is stored in a local text file named **`knowledge_base.txt`**.

- The document is split into chunks of approximately **800 characters**.
- Chunking helps limit the amount of text sent to the AI model.
- This improves relevance and reduces token usage.

This approach reflects how larger documents are typically handled in real-world AI systems.



## 5. Chunk Selection Logic (Keyword-Based Filtering)

To determine the most relevant section of the knowledge base:

- The user’s question is split into keywords.
- Each chunk is scored based on how many keywords it contains.
- The chunk with the highest score is selected and used as context.

This simple keyword-based approach satisfies the assignment requirement for basic filtering and helps reduce unnecessary data sent to the AI.



## 6. Prompt Design Explanation

### Prompt Structure
The prompt sent to the AI consists of two parts:
- **System message:** Defines the AI as an IT support assistant.
- **User message:** Contains the selected knowledge base chunk and the user’s question.

### Why This Structure Was Chosen
- Clearly defines the role of the AI.
- Provides relevant context from the knowledge base.
- Keeps the prompt focused and easy for the model to interpret.

### How This Ensures Accurate Answers
- Only relevant information is provided as context.
- The AI is guided to answer using the supplied document.
- Limiting context reduces the risk of hallucinated or unrelated answers.



## 7. Error Handling

Basic error handling is implemented for:
- Missing knowledge base file
- Empty user input
- API request failures
- Unexpected exceptions during API calls

When an error occurs, the application displays a clear message instead of crashing.



## 8. Assumptions and Limitations

### Assumptions
- The knowledge base is of moderate size.
- User questions are related to IT topics contained in the document.
- A valid API key is available when AI-generated answers are required.

### Limitations
- Keyword matching is simple and may not always select the best possible chunk.
- Only one chunk is used as context.
- No caching of previous questions or answers.
- Console-based user interface only.

These limitations were accepted to keep the solution simple and aligned with the assignment scope.



## 9. Improvement Ideas

Possible future improvements include:
- Using semantic search or embeddings instead of keyword matching.
- Caching previous questions and answers to reduce API calls.
- Adding logging for user questions and API responses.
- Implementing more detailed error handling.
- Converting the application into a simple web-based UI.



## 10. Conclusion

This project demonstrates:
- Basic C# and .NET programming skills
- File handling and string processing
- Simple information retrieval using keyword matching
- Secure integration with an external AI API


