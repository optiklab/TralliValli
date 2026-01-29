/**
 * SettingsPage Component
 *
 * Displays user settings including notification preferences placeholder
 * and theme toggle for dark/light mode.
 */

import { useThemeStore } from '@stores';

export interface SettingsPageProps {
  onThemeChange?: (theme: 'light' | 'dark') => void;
}

export function SettingsPage({ onThemeChange }: SettingsPageProps) {
  const { theme, toggleTheme } = useThemeStore();

  const handleThemeToggle = () => {
    toggleTheme();
    const newTheme = theme === 'light' ? 'dark' : 'light';
    onThemeChange?.(newTheme);
  };

  return (
    <div className="min-h-screen bg-gray-50 py-6 px-4">
      <div className="max-w-3xl mx-auto">
        <h1 className="text-3xl font-bold text-gray-900 mb-6">Settings</h1>

        <div className="space-y-6">
          {/* Theme Settings */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Appearance</h2>

            <div className="flex items-center justify-between">
              <div>
                <label htmlFor="theme-toggle" className="text-sm font-medium text-gray-700">
                  Dark Mode
                </label>
                <p className="text-sm text-gray-500">
                  Switch between light and dark theme
                </p>
              </div>

              <button
                id="theme-toggle"
                role="switch"
                aria-checked={theme === 'dark'}
                onClick={handleThemeToggle}
                className={`relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 ${
                  theme === 'dark' ? 'bg-indigo-600' : 'bg-gray-200'
                }`}
              >
                <span
                  className={`pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out ${
                    theme === 'dark' ? 'translate-x-5' : 'translate-x-0'
                  }`}
                />
              </button>
            </div>

            <div className="mt-2 text-sm text-gray-600">
              Current theme: <span className="font-medium capitalize">{theme}</span>
            </div>
          </div>

          {/* Notification Settings Placeholder */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Notifications</h2>

            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <label htmlFor="email-notifications" className="text-sm font-medium text-gray-700">
                    Email Notifications
                  </label>
                  <p className="text-sm text-gray-500">
                    Receive email notifications for new messages
                  </p>
                </div>
                <div className="text-sm text-gray-400 italic">Coming soon</div>
              </div>

              <div className="flex items-center justify-between">
                <div>
                  <label htmlFor="push-notifications" className="text-sm font-medium text-gray-700">
                    Push Notifications
                  </label>
                  <p className="text-sm text-gray-500">
                    Receive push notifications for new messages
                  </p>
                </div>
                <div className="text-sm text-gray-400 italic">Coming soon</div>
              </div>

              <div className="flex items-center justify-between">
                <div>
                  <label htmlFor="sound-notifications" className="text-sm font-medium text-gray-700">
                    Sound Notifications
                  </label>
                  <p className="text-sm text-gray-500">
                    Play a sound when you receive a new message
                  </p>
                </div>
                <div className="text-sm text-gray-400 italic">Coming soon</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
