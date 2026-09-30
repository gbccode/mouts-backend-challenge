import { FormArray, FormControl, FormGroup } from '@angular/forms';
import { activeItemsValidator, localDateTimeValidator, toLocalDateTime } from './sales.helpers';

describe('Sale form validation and dates', () => {
  it('round-trips UTC values through the local timezone without losing seconds or milliseconds', () => {
    const local = new Date(2026, 8, 30, 9, 30, 12, 345);
    const value = toLocalDateTime(local.toISOString());
    expect(value).toBe('2026-09-30T09:30:12.345');
    expect(new Date(value).toISOString()).toBe(local.toISOString());
  });

  it('handles a local date that falls on a different day from UTC', () => {
    const local = new Date(2026, 8, 30, 23, 45);
    expect(toLocalDateTime(local.toISOString())).toBe('2026-09-30T23:45:00.000');
  });

  it('rejects invalid dates and accepts minute or second precision local inputs', () => {
    for (const value of ['invalid', '2026-02-30T12:00', '2026-09-30T25:00', '2026-09-30T09:00Z']) {
      expect(localDateTimeValidator(new FormControl(value))).withContext(value).toEqual({ localDateTime: true });
    }
    for (const value of ['2026-09-30T09:00', '2026-09-30T09:00:12', '2026-09-30T09:00:12.3']) {
      expect(localDateTimeValidator(new FormControl(value))).withContext(value).toBeNull();
    }
    expect(toLocalDateTime('invalid')).toBe('');
  });

  it('compares GUIDs case-insensitively and ignores disabled rows', () => {
    const rows = new FormArray([
      new FormGroup({ productId: new FormControl('abcdef') }),
      new FormGroup({ productId: new FormControl('ABCDEF') }),
    ], activeItemsValidator);
    expect(rows.hasError('duplicateProducts')).toBeTrue();
    rows.at(1).disable();
    expect(rows.hasError('duplicateProducts')).toBeFalse();
    rows.clear();
    expect(rows.hasError('noActiveItems')).toBeTrue();
  });
});
