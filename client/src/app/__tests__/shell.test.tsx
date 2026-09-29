import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import AppShell from '../AppShell';

describe('AppShell', () => {
  it('renders the Arabic shell with navigation and the routed outlet', () => {
    render(
      <MemoryRouter initialEntries={['/']}>
        <Routes>
          <Route element={<AppShell />}>
            <Route index element={<div>محتوى الصفحة</div>} />
          </Route>
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByText('خيوط')).toBeInTheDocument();
    expect(screen.getByText('محتوى الصفحة')).toBeInTheDocument();

    for (const label of ['الرئيسية', 'الكتالوج', 'الطلبات', 'العينات', 'حسابي']) {
      expect(screen.getByRole('link', { name: label })).toBeInTheDocument();
    }
  });
});
