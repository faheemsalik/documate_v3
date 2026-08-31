import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  beforeEach(() => {
    localStorage.removeItem('documate.theme');
    document.documentElement.classList.remove('app-dark');
    TestBed.configureTestingModule({});
  });

  it('defaults to dark and applies app-dark class', () => {
    const theme = TestBed.inject(ThemeService);
    expect(theme.mode()).toBe('dark');
    expect(document.documentElement.classList.contains('app-dark')).toBe(true);
  });

  it('toggles to light', () => {
    const theme = TestBed.inject(ThemeService);
    theme.toggle();
    expect(theme.mode()).toBe('light');
    expect(document.documentElement.classList.contains('app-dark')).toBe(false);
  });
});
