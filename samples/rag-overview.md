# Retrieval-Augmented Generation

Retrieval-Augmented Generation (RAG) combines information retrieval with a generative language model. The goal is to ground an answer in documents supplied by the application instead of relying only on the model's training data.

## Pipeline

During ingestion, documents are normalized, divided into overlapping chunks, converted into embedding vectors, and stored with their metadata. Overlap helps preserve meaning across chunk boundaries.

During question answering, the question is embedded with the same model. A vector similarity search selects the closest chunks. Those chunks are passed to the chat model as context, together with instructions to answer only from the retrieved evidence.

Returning the selected chunks makes the result auditable and lets clients display citations or inspect why an answer was produced.
