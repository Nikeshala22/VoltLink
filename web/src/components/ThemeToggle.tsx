// -----------------------------------------------------------------------------
// File        : components/ThemeToggle.tsx
// Project     : VoltLink Web - Smart Solar Microgrid Trading System
// Description : Three way switch between the light theme, the dark theme and
//               following the operating system.
// Author      : <IT Number - Member Name>
// Created     : 2026-09-03
// -----------------------------------------------------------------------------

import { useTheme } from '../context/useTheme'
import type { ThemePreference } from '../context/ThemeContext'
import { IconMonitor, IconMoon, IconSun } from './Icons'
import type { ComponentType, SVGProps } from 'react'

interface Option {
  value: ThemePreference
  label: string
  Icon: ComponentType<SVGProps<SVGSVGElement>>
}

const OPTIONS: Option[] = [
  { value: 'light', label: 'Light theme', Icon: IconSun },
  { value: 'dark', label: 'Dark theme', Icon: IconMoon },
  { value: 'system', label: 'Match system', Icon: IconMonitor },
]

/**
 * A small segmented control. Showing all three choices at once makes the
 * current setting obvious, which a single cycling button does not.
 */
export default function ThemeToggle() {
  const { preference, setPreference } = useTheme()

  return (
    <div
      role="radiogroup"
      aria-label="Colour theme"
      className="inline-flex items-center gap-0.5 rounded-xl border border-line bg-surface p-1"
    >
      {OPTIONS.map((option) => {
        const isActive = preference === option.value

        return (
          <button
            key={option.value}
            type="button"
            role="radio"
            aria-checked={isActive}
            aria-label={option.label}
            title={option.label}
            onClick={() => setPreference(option.value)}
            className={`flex h-7 w-7 items-center justify-center rounded-lg
                        transition-colors duration-150 ${
                          isActive
                            ? 'bg-brand-soft text-brand-soft-fg'
                            : 'text-ink-400 hover:bg-surface-2 hover:text-ink-700'
                        }`}
          >
            <option.Icon className="h-[15px] w-[15px]" />
          </button>
        )
      })}
    </div>
  )
}
