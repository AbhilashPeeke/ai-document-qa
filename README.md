# AI Document Q&A Assistant

Upload a PDF or text file and chat with it. The app answers questions using **only the content of your document** and shows the exact passages it used as sources.

Built with **ASP.NET Core 8 Web API**, **Angular**, and the **Google Gemini API**, using a **RAG (Retrieval-Augmented Generation)** pipeline.

![Screenshot](docs/screenshot.png)

## Features

- Upload PDF or TXT files (drag and drop or browse)
- Ask questions in a modern dark chat interface
- Answers are grounded in the document, not guessed
- Collapsible **Sources** under each answer showing the chunks used
- Replies "I couldn't find that in the document" when the answer isn't there

## How it works (RAG pipeline)

1. **Extract:** the API reads text from the uploaded file (PdfPig for PDFs).
2. **Chunk:** the text is split into ~1,000-character pieces with 200 characters of overlap, so sentences cut at a boundary still appear whole in one chunk.
3. **Retrieve:** for each question, chunks are ranked with a TF-IDF-style keyword score, where rare words count for more. The top 4 are selected.
4. **Augment:** only those chunks are placed in a prompt that tells the model to answer from the context alone.
5. **Generate:** the prompt is sent to Gemini, and the answer is returned to Angular with its sources.

## Tech stack

| Layer | Technology |
|---|---|
| Frontend | Angular (standalone components, signals), TypeScript |
| Backend | ASP.NET Core 8 Web API, C# |
| AI model | Google Gemini API |
| PDF parsing | PdfPig |
| Secrets | .NET User Secrets |

## Project structure

```
DocQaApi/        ASP.NET Core Web API (controllers, Gemini service, chunker, search)
doc-qa-app/      Angular frontend
docs/            Screenshots
```

## Getting started

### Prerequisites

- .NET 8 SDK
- Node.js (LTS) and Angular CLI (`npm install -g @angular/cli`)
- A free Gemini API key from [Google AI Studio](https://aistudio.google.com)

### 1. Run the API

```bash
cd DocQaApi
dotnet user-secrets set "Gemini:ApiKey" "YOUR_API_KEY"
dotnet run
```

Note the HTTPS port shown in the console (for example `https://localhost:44365`).

### 2. Run the frontend

```bash
cd doc-qa-app
npm install
ng serve
```

Open `http://localhost:4200`. If your API port is different, change the `api` URL in `doc-qa-app/src/app/app.component.ts`.

### Model name

The model is set in `DocQaApi/appsettings.json` under `Gemini:Model`. Free-tier model names change over time, so if you get a 404 model error, use a model listed in AI Studio.

## Limitations and roadmap

- Search is keyword-based, so it matches words rather than meaning (for example "salary" won't find "compensation").
- The document is stored in memory and cleared when the API restarts.
- Scanned PDFs (images only) are not supported.

Planned improvements:

- [ ] Embeddings for semantic search
- [ ] Persist documents and chat history in SQL Server
- [ ] Render markdown in answers
- [ ] Support multiple documents

## Privacy note

When using the free Gemini tier, content sent to the API may be used by Google to improve its products. Don't upload confidential documents.
