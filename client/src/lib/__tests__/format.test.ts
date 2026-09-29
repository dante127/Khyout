import { describe, expect, it } from 'vitest';
import {
  daysLeft,
  fiberLabel,
  formatDate,
  formatMoney,
  formatNumber,
  mediaUrl,
  unitLabel,
  weaveLabel,
} from '../format';

describe('format helpers', () => {
  it('maps enum labels with raw-value fallback', () => {
    expect(unitLabel('Kg')).toBe('كجم');
    expect(unitLabel('Meter')).toBe('متر');
    expect(unitLabel('Unknown')).toBe('Unknown');
    expect(fiberLabel('Cotton')).toBe('قطن');
    expect(weaveLabel('Twill')).toBe('مبرد');
    expect(weaveLabel(null)).toBe('');
  });

  it('formats money with a placeholder for missing values', () => {
    expect(formatMoney(null, 'USD')).toBe('—');
    expect(formatMoney(12.5, 'USD')).toBe('12.5 USD');
    expect(formatMoney(1200, null)).toBe('1,200');
  });

  it('formats numbers with grouping', () => {
    expect(formatNumber(1234.567)).toBe('1,234.57');
    expect(formatNumber(98, 1)).toBe('98');
  });

  it('formats dates and rejects invalid input', () => {
    expect(formatDate(null)).toBe('—');
    expect(formatDate('not-a-date')).toBe('—');
    expect(formatDate('2026-01-05T00:00:00Z')).toContain('2026');
  });

  it('computes remaining days by ceiling', () => {
    const iso = new Date(Date.now() + 2.5 * 86_400_000).toISOString();
    expect(daysLeft(iso)).toBe(3);
  });

  it('maps stored media paths to public URLs', () => {
    expect(mediaUrl('abc123.webp')).toBe('/media/abc123.webp');
    expect(mediaUrl('/media/abc123.webp')).toBe('/media/abc123.webp');
    expect(mediaUrl('https://cdn.example.com/i.webp')).toBe('https://cdn.example.com/i.webp');
  });
});
