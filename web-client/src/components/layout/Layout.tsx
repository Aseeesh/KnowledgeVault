import { Outlet, Link, useLocation } from 'react-router-dom';
import { MessageSquare, FileText, LayoutDashboard, Search, Sun, Moon, Database } from 'lucide-react';
import { clsx } from 'clsx';
import { useAppStore } from '../../store';

const nav = [
  { to: '/', label: 'Chat', icon: MessageSquare },
  { to: '/search', label: 'Search', icon: Search },
  { to: '/documents', label: 'Documents', icon: FileText },
  { to: '/dashboard', label: 'Dashboard', icon: LayoutDashboard },
];

export default function Layout() {
  const location = useLocation();
  const { theme, toggleTheme } = useAppStore();

  return (
    <div className={clsx('min-h-screen flex', theme === 'light' && 'bg-white text-gray-900')}>
      <aside className={clsx(
        'w-64 flex flex-col border-r',
        theme === 'dark' ? 'bg-gray-900 border-gray-800' : 'bg-gray-50 border-gray-200'
      )}>
        <div className={clsx('p-5 border-b', theme === 'dark' ? 'border-gray-800' : 'border-gray-200')}>
          <div className="flex items-center gap-2">
            <Database className="text-indigo-500" size={24} />
            <h1 className="text-lg font-bold">KnowledgeVault</h1>
          </div>
          <p className={clsx('text-xs mt-1', theme === 'dark' ? 'text-gray-500' : 'text-gray-400')}>
            RAG-Powered Knowledge Search
          </p>
        </div>

        <nav className="flex-1 p-3 space-y-1">
          {nav.map(({ to, label, icon: Icon }) => (
            <Link
              key={to}
              to={to}
              className={clsx(
                'flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm transition-colors',
                location.pathname === to
                  ? 'bg-indigo-600/20 text-indigo-400 font-medium'
                  : theme === 'dark'
                    ? 'text-gray-400 hover:bg-gray-800 hover:text-gray-200'
                    : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900'
              )}
            >
              <Icon size={18} />
              {label}
            </Link>
          ))}
        </nav>

        <div className={clsx('p-3 border-t', theme === 'dark' ? 'border-gray-800' : 'border-gray-200')}>
          <button
            onClick={toggleTheme}
            className={clsx(
              'flex items-center gap-2 w-full px-3 py-2 rounded-lg text-sm transition-colors',
              theme === 'dark' ? 'text-gray-400 hover:bg-gray-800' : 'text-gray-600 hover:bg-gray-100'
            )}
          >
            {theme === 'dark' ? <Sun size={16} /> : <Moon size={16} />}
            {theme === 'dark' ? 'Light Mode' : 'Dark Mode'}
          </button>
        </div>
      </aside>

      <main className="flex-1 flex flex-col overflow-hidden">
        <Outlet />
      </main>
    </div>
  );
}
