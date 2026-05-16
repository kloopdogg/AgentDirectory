import { Component, Output, EventEmitter, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-file-drop',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  template: `
    <div
      class="drop-zone"
      [class.drag-over]="isDragging"
      (dragenter)="onDragEnter($event)"
      (dragover)="onDragOver($event)"
      (dragleave)="onDragLeave($event)"
      (drop)="onDrop($event)">
      <ng-content />
    </div>
  `,
  styles: [`
    .drop-zone {
      position: relative;
      display: flex;
      flex-direction: column;
      flex: 1;
      min-height: 0;
      overflow: hidden;
    }
    .drop-zone.drag-over::after {
      content: 'Drop file here';
      position: absolute;
      inset: 0;
      background: rgba(63, 81, 181, 0.12);
      border: 2px dashed #3f51b5;
      border-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1.2rem;
      color: #3f51b5;
      font-weight: 600;
      pointer-events: none;
      z-index: 10;
    }
  `],
})
export class FileDropComponent {
  @Output() filesDropped = new EventEmitter<File[]>();

  isDragging = false;

  onDragEnter(e: DragEvent) { e.preventDefault(); this.isDragging = true; }
  onDragOver(e: DragEvent) { e.preventDefault(); }
  onDragLeave(e: DragEvent) { this.isDragging = false; }

  onDrop(e: DragEvent) {
    e.preventDefault();
    this.isDragging = false;
    const files = Array.from(e.dataTransfer?.files ?? []);
    if (files.length > 0) this.filesDropped.emit(files);
  }
}
