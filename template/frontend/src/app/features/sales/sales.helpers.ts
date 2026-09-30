import { FormArray, ValidatorFn } from '@angular/forms';

export const integerValidator: ValidatorFn = (control) => {
  return control.value == null || control.value === '' || Number.isInteger(control.value)
    ? null : { integer: true };
};

export const moneyValidator: ValidatorFn = (control) => {
  const value = control.value;
  if (value == null || value === '') return null;
  return typeof value === 'number' && Number.isFinite(value)
    && Math.abs(value * 100 - Math.round(value * 100)) < 1e-8
    ? null : { money: true };
};

export const activeItemsValidator: ValidatorFn = (control) => {
  if (!(control instanceof FormArray)) return null;
  const rows = control.controls.filter((row) => row.enabled);
  if (rows.length === 0) return { noActiveItems: true };
  const ids = rows.map((row) => String(row.get('productId')?.value ?? '').trim().toLowerCase()).filter(Boolean);
  return new Set(ids).size === ids.length ? null : { duplicateProducts: true };
};

/** Uses local calendar fields, never a sliced UTC ISO timestamp. */
export function toLocalDateTime(iso: string): string {
  const date = new Date(iso);
  if (!Number.isFinite(date.getTime())) return '';
  const pad = (value: number, width = 2) => String(value).padStart(width, '0');
  return `${pad(date.getFullYear(), 4)}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
    + `T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}.${pad(date.getMilliseconds(), 3)}`;
}

export const localDateTimeValidator: ValidatorFn = (control) => {
  if (!control.value) return null;
  const value = String(control.value);
  if (!/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(:\d{2}(\.\d{1,3})?)?$/.test(value)) return { localDateTime: true };
  const date = new Date(value);
  if (!Number.isFinite(date.getTime())) return { localDateTime: true };
  // Reject impossible dates and times normalized across a daylight-saving gap.
  const normalized = toLocalDateTime(date.toISOString());
  const expected = value.length === 16 ? `${value}:00.000`
    : value.length === 19 ? `${value}.000` : value.padEnd(23, '0');
  return normalized === expected ? null : { localDateTime: true };
};

export function apiErrorMessage(error: unknown, fallback: string): string {
  const payload = (error as { error?: { detail?: unknown; message?: unknown } } | null)?.error;
  const message = payload?.detail ?? payload?.message;
  return typeof message === 'string' && message.trim() ? message : fallback;
}
