import {
  AfterViewInit,
  Directive,
  ElementRef,
  Input,
  OnDestroy,
  forwardRef,
  inject,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import flatpickr from 'flatpickr';
import { Danish } from 'flatpickr/dist/l10n/da.js';
import type { Instance } from 'flatpickr/dist/types/instance';

// Genanvendelig dato-/datotidsvælger, stylet til at matche appens
// designsystem (se .flatpickr-* overrides i styles.scss). Bruges som
// [appFlatpickr] direkte på et formControlName-input.
@Directive({
  selector: '[appFlatpickr]',
  standalone: true,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => FlatpickrDirective),
      multi: true,
    },
  ],
})
export class FlatpickrDirective implements AfterViewInit, OnDestroy, ControlValueAccessor {
  @Input() enableTime = false;
  @Input() minDate: string | Date = 'today';

  private readonly elementRef = inject(ElementRef<HTMLInputElement>);
  private instance?: Instance;
  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  ngAfterViewInit(): void {
    this.instance = flatpickr(this.elementRef.nativeElement, {
      enableTime: this.enableTime,
      time_24hr: true,
      dateFormat: this.enableTime ? "Y-m-d\\TH:i" : 'Y-m-d',
      altInput: true,
      altInputClass: 'flatpickr-alt-input',
      altFormat: this.enableTime ? 'd. F Y, H:i' : 'd. F Y',
      locale: Danish,
      minDate: this.minDate,
      onChange: (_selectedDates, dateStr) => {
        this.onChange(dateStr);
        this.onTouched();
      },
    });
  }

  writeValue(value: string): void {
    this.instance?.setDate(value, false);
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.instance?.set('clickOpens', !isDisabled);
  }

  ngOnDestroy(): void {
    this.instance?.destroy();
  }
}
