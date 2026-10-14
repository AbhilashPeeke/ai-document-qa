import { Component, ElementRef, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

interface Source {
  chunkNumber: number;
  text: string;
}

interface ChatMessage {
  role: 'user' | 'ai';
  text: string;
  sources?: Source[];
}

interface UploadResult {
  fileName: string;
  characters: number;
  chunkCount: number;
  preview: string;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="app">
      <header>
        <div class="brand"><span class="logo">✦</span>AI Document Q&A Assistant</div>
        @if (uploadInfo()) {
          <div class="doc-chip">
            <span class="dot"></span>
            {{ uploadInfo()!.fileName }} · {{ uploadInfo()!.chunkCount }} chunks
            <button class="link" (click)="fileInput.click()">Change</button>
          </div>
        }
      </header>

      <input #fileInput type="file" accept=".pdf,.txt" hidden (change)="onFileSelected($event)" />

      <main #scroller>
        <div class="content">
          @if (!uploadInfo()) {
            <div
              class="drop"
              [class.over]="dragging()"
              (click)="fileInput.click()"
              (dragover)="onDragOver($event)"
              (dragleave)="dragging.set(false)"
              (drop)="onDrop($event)"
            >
              <div class="drop-icon">📄</div>
              <h2>Chat with your documents</h2>
              <p>Drop a PDF or TXT file here, or click to browse</p>
              @if (uploading()) { <p class="hint">Reading your document...</p> }
              @if (uploadError()) { <p class="err">{{ uploadError() }}</p> }
            </div>
          } @else if (messages().length === 0) {
            <div class="empty">
              <div class="logo big">✦</div>
              <h2>Your document is ready</h2>
              <p>Ask a question below. Tip: use words that appear in your document.</p>
            </div>
          }

          @for (m of messages(); track $index) {
            <div class="row" [class.user]="m.role === 'user'">
              <div class="avatar">{{ m.role === 'user' ? 'You' : '✦' }}</div>
              <div class="bubble">
                <div class="text">{{ m.text }}</div>
                @if (m.sources && m.sources.length > 0) {
                  <details class="sources">
                    <summary>Sources ({{ m.sources.length }})</summary>
                    @for (s of m.sources; track s.chunkNumber) {
                      <div class="source"><b>Chunk {{ s.chunkNumber }}</b> {{ s.text }}</div>
                    }
                  </details>
                }
              </div>
            </div>
          }

          @if (loading()) {
            <div class="row">
              <div class="avatar">✦</div>
              <div class="bubble typing"><span></span><span></span><span></span></div>
            </div>
          }
        </div>
      </main>

      <footer>
        <div class="input-wrap">
          <input
            [(ngModel)]="question"
            (keyup.enter)="send()"
            [disabled]="!uploadInfo()"
            [placeholder]="uploadInfo() ? 'Ask a question about your document...' : 'Upload a document to start'"
          />
          <button class="send" (click)="send()" [disabled]="loading() || !uploadInfo() || !question.trim()">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                 stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
              <path d="M12 19V5M5 12l7-7 7 7"/>
            </svg>
          </button>
        </div>
        <p class="note">Answers come only from your uploaded document.</p>
      </footer>
    </div>
  `,
  styles: [`
    :host { display: block; height: 100vh; color: #e6e8ee;
            font-family: 'Segoe UI', Roboto, Arial, sans-serif; }
    .app { display: flex; flex-direction: column; height: 100%; }

    header { display: flex; justify-content: space-between; align-items: center;
             padding: 14px 24px; border-bottom: 1px solid #23262f; }
    .brand { font-weight: 600; font-size: 18px; display: flex; align-items: center; gap: 8px; }
    .logo { background: linear-gradient(135deg, #7c5cff, #3ea6ff); color: #fff;
            width: 28px; height: 28px; border-radius: 8px; display: grid; place-items: center;
            font-size: 15px; }
    .logo.big { width: 56px; height: 56px; font-size: 28px; border-radius: 16px; margin: 0 auto 12px; }
    .doc-chip { display: flex; align-items: center; gap: 8px; background: #181b23;
                border: 1px solid #2a2e39; padding: 6px 12px; border-radius: 999px; font-size: 13px; }
    .dot { width: 8px; height: 8px; border-radius: 50%; background: #3ddc84; }
    .link { background: none; border: none; color: #7aa7ff; cursor: pointer; font-size: 13px; }

    main { flex: 1; overflow-y: auto; }
    .content { max-width: 780px; margin: 0 auto; padding: 24px 16px; }

    .drop { margin-top: 8vh; border: 2px dashed #2f3441; border-radius: 20px; padding: 56px 24px;
            text-align: center; cursor: pointer; transition: .2s; background: #131620; }
    .drop:hover, .drop.over { border-color: #7c5cff; background: #171a27; }
    .drop-icon { font-size: 44px; }
    .drop h2, .empty h2 { margin: 12px 0 6px; }
    .drop p, .empty p { color: #8b90a0; margin: 4px 0; }
    .hint { color: #7aa7ff !important; }
    .err { color: #ff6b6b !important; }
    .empty { text-align: center; margin-top: 14vh; }

    .row { display: flex; gap: 12px; margin-bottom: 20px; align-items: flex-start; }
    .row.user { flex-direction: row-reverse; }
    .avatar { flex: none; width: 34px; height: 34px; border-radius: 50%; display: grid;
              place-items: center; font-size: 12px; font-weight: 600;
              background: linear-gradient(135deg, #7c5cff, #3ea6ff); color: #fff; }
    .row.user .avatar { background: #2a2e39; color: #cfd3e0; }
    .bubble { background: #181b23; border: 1px solid #23262f; padding: 12px 16px;
              border-radius: 16px; max-width: 80%; line-height: 1.55; }
    .row.user .bubble { background: linear-gradient(135deg, #5b45d6, #3a7de0); border: none; color: #fff; }
    .text { white-space: pre-wrap; word-break: break-word; }

    .sources { margin-top: 10px; font-size: 12.5px; }
    .sources summary { cursor: pointer; color: #7aa7ff; }
    .source { margin-top: 8px; padding: 8px 10px; background: #11141b; border-radius: 10px;
              color: #a3a8b8; }
    .source b { color: #cfd3e0; margin-right: 6px; }

    .typing { display: flex; gap: 5px; padding: 16px 18px; }
    .typing span { width: 7px; height: 7px; border-radius: 50%; background: #7a7f93;
                   animation: bounce 1.2s infinite ease-in-out; }
    .typing span:nth-child(2) { animation-delay: .15s; }
    .typing span:nth-child(3) { animation-delay: .3s; }
    @keyframes bounce { 0%, 80%, 100% { transform: translateY(0); opacity: .5; }
                        40% { transform: translateY(-6px); opacity: 1; } }

    footer { padding: 12px 16px 16px; }
    .input-wrap { max-width: 780px; margin: 0 auto; display: flex; align-items: center;
                  background: #181b23; border: 1px solid #2a2e39; border-radius: 999px;
                  padding: 6px 6px 6px 20px; }
    .input-wrap:focus-within { border-color: #7c5cff; }
    .input-wrap input { flex: 1; background: none; border: none; outline: none;
                        color: #e6e8ee; font-size: 15px; padding: 8px 0; }
    .send { width: 38px; height: 38px; border-radius: 50%; border: none; cursor: pointer;
            background: linear-gradient(135deg, #7c5cff, #3ea6ff); color: #fff;
            display: grid; place-items: center; }
    .send:disabled { opacity: .35; cursor: not-allowed; }
    .note { text-align: center; color: #6b7080; font-size: 12px; margin: 8px 0 0; }
  `]
})
export class AppComponent {
  private http = inject(HttpClient);
  private api = 'https://localhost:44365/api';

  @ViewChild('scroller') scroller!: ElementRef<HTMLElement>;

  messages = signal<ChatMessage[]>([]);
  loading = signal(false);
  question = '';

  uploading = signal(false);
  uploadInfo = signal<UploadResult | null>(null);
  uploadError = signal('');
  dragging = signal(false);

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) this.upload(file);
    input.value = '';
  }

  onDragOver(event: DragEvent) {
    event.preventDefault();
    this.dragging.set(true);
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    this.dragging.set(false);
    const file = event.dataTransfer?.files?.[0];
    if (file) this.upload(file);
  }

  private upload(file: File) {
    const form = new FormData();
    form.append('file', file);

    this.uploading.set(true);
    this.uploadError.set('');
    this.messages.set([]);

    this.http.post<UploadResult>(`${this.api}/documents/upload`, form).subscribe({
      next: res => {
        this.uploadInfo.set(res);
        this.uploading.set(false);
      },
      error: err => {
        this.uploadInfo.set(null);
        this.uploadError.set(err?.error?.error ?? 'Upload failed. Is the API running?');
        this.uploading.set(false);
      }
    });
  }

  send() {
    const q = this.question.trim();
    if (!q || this.loading() || !this.uploadInfo()) return;

    this.messages.update(m => [...m, { role: 'user', text: q }]);
    this.question = '';
    this.loading.set(true);
    this.scrollToBottom();

    this.http
      .post<{ answer: string; sources: Source[] }>(`${this.api}/chat`, { question: q })
      .subscribe({
        next: res => {
          this.messages.update(m => [...m, { role: 'ai', text: res.answer, sources: res.sources }]);
          this.loading.set(false);
          this.scrollToBottom();
        },
        error: err => {
          const detail = err?.error?.error ?? 'Could not reach the API.';
          this.messages.update(m => [...m, { role: 'ai', text: 'Error: ' + detail }]);
          this.loading.set(false);
          this.scrollToBottom();
        }
      });
  }

  private scrollToBottom() {
    setTimeout(() => {
      const el = this.scroller?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    }, 50);
  }
}