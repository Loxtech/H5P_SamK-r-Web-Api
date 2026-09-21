import { Component, HostListener, input, output } from '@angular/core';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  templateUrl: './confirm-dialog.component.html',
  styleUrl: './confirm-dialog.component.scss',
})
export class ConfirmDialogComponent {
  readonly open = input(false);
  readonly title = input('Er du sikker?');
  readonly message = input('');
  readonly confirmLabel = input('Bekræft');
  readonly cancelLabel = input('Annuller');
  readonly danger = input(false);

  readonly confirmed = output<void>();
  readonly cancelled = output<void>();

  onCancel(): void {
    this.cancelled.emit();
  }

  onConfirm(): void {
    this.confirmed.emit();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.open()) {
      this.cancelled.emit();
    }
  }
}
