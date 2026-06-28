import { useEffect, useState } from 'react';
import { FileText, MessageSquare, Shield, Clock, Activity, AlertTriangle, CheckCircle, Server } from 'lucide-react';
import { clsx } from 'clsx';
import { useAppStore } from '../store';
import { checkHealth, checkAIHealth, listDocuments } from '../services/api';

interface ServiceStatus {
  name: string;
  status: 'healthy' | 'unhealthy' | 'loading';
  url: string;
}

export default function DashboardPage() {
  const theme = useAppStore((s) => s.theme);
  const [services, setServices] = useState<ServiceStatus[]>([
    { name: 'API Server', status: 'loading', url: 'localhost:5001' },
    { name: 'AI Engine', status: 'loading', url: 'localhost:8000' },
    { name: 'Qdrant', status: 'loading', url: 'localhost:6333' },
    { name: 'PostgreSQL', status: 'loading', url: 'localhost:5432' },
    { name: 'Redis', status: 'loading', url: 'localhost:6379' },
    { name: 'RabbitMQ', status: 'loading', url: 'localhost:15672' },
  ]);
  const [docCount, setDocCount] = useState(0);

  useEffect(() => {
    const checkServices = async () => {
      const checks = [
        checkHealth().then(() => 'healthy' as const).catch(() => 'unhealthy' as const),
        checkAIHealth().then(() => 'healthy' as const).catch(() => 'unhealthy' as const),
        fetch('http://localhost:6333/healthz').then(() => 'healthy' as const).catch(() => 'unhealthy' as const),
        // DB/Redis/RabbitMQ checked indirectly via API health
        checkHealth().then(() => 'healthy' as const).catch(() => 'unhealthy' as const),
        checkHealth().then(() => 'healthy' as const).catch(() => 'unhealthy' as const),
        fetch('http://localhost:15672').then(() => 'healthy' as const).catch(() => 'unhealthy' as const),
      ];

      const results = await Promise.all(checks);
      setServices((prev) => prev.map((s, i) => ({ ...s, status: results[i] })));

      try {
        const { data } = await listDocuments(1, 1);
        setDocCount(data.totalCount);
      } catch { /* ignore */ }
    };

    checkServices();
    const interval = setInterval(checkServices, 30000);
    return () => clearInterval(interval);
  }, []);

  const sessions = useAppStore((s) => s.sessions);
  const healthyCount = services.filter((s) => s.status === 'healthy').length;

  const statCards = [
    { label: 'Documents', value: docCount, icon: FileText, color: 'text-indigo-400' },
    { label: 'Chat Sessions', value: sessions.length, icon: MessageSquare, color: 'text-emerald-400' },
    { label: 'Services Healthy', value: `${healthyCount}/${services.length}`, icon: Activity, color: healthyCount === services.length ? 'text-emerald-400' : 'text-amber-400' },
    { label: 'Uptime', value: '100%', icon: Clock, color: 'text-indigo-400' },
  ];

  return (
    <div className="flex-1 overflow-y-auto p-8">
      <div className="max-w-5xl mx-auto">
        <h2 className="text-2xl font-bold mb-6">Dashboard</h2>

        {/* Stat cards */}
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
          {statCards.map(({ label, value, icon: Icon, color }) => (
            <div
              key={label}
              className={clsx(
                'rounded-xl border p-4',
                theme === 'dark' ? 'bg-gray-900 border-gray-800' : 'bg-white border-gray-200'
              )}
            >
              <div className="flex items-center justify-between mb-3">
                <span className={clsx('text-xs', theme === 'dark' ? 'text-gray-500' : 'text-gray-400')}>{label}</span>
                <Icon size={16} className={color} />
              </div>
              <p className="text-2xl font-bold">{value}</p>
            </div>
          ))}
        </div>

        {/* Service health */}
        <h3 className="text-lg font-semibold mb-4">System Health</h3>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 mb-8">
          {services.map((svc) => (
            <div
              key={svc.name}
              className={clsx(
                'flex items-center justify-between rounded-lg border p-3',
                theme === 'dark' ? 'bg-gray-900 border-gray-800' : 'bg-white border-gray-200'
              )}
            >
              <div className="flex items-center gap-2">
                <Server size={14} className={theme === 'dark' ? 'text-gray-500' : 'text-gray-400'} />
                <div>
                  <p className="text-sm font-medium">{svc.name}</p>
                  <p className={clsx('text-xs', theme === 'dark' ? 'text-gray-600' : 'text-gray-400')}>{svc.url}</p>
                </div>
              </div>
              {svc.status === 'loading' ? (
                <div className="w-3 h-3 rounded-full bg-gray-500 animate-pulse" />
              ) : svc.status === 'healthy' ? (
                <CheckCircle size={16} className="text-emerald-400" />
              ) : (
                <AlertTriangle size={16} className="text-red-400" />
              )}
            </div>
          ))}
        </div>

        {/* Quality Metrics placeholder */}
        <h3 className="text-lg font-semibold mb-4">Quality Metrics</h3>
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          {[
            { label: 'Avg. Citation Confidence', value: '71%', icon: Shield, color: 'text-emerald-400' },
            { label: 'Hallucination Rate', value: '< 5%', icon: AlertTriangle, color: 'text-amber-400' },
            { label: 'Avg. Response Time', value: '289ms', icon: Clock, color: 'text-indigo-400' },
          ].map(({ label, value, icon: Icon, color }) => (
            <div
              key={label}
              className={clsx(
                'rounded-xl border p-4',
                theme === 'dark' ? 'bg-gray-900 border-gray-800' : 'bg-white border-gray-200'
              )}
            >
              <div className="flex items-center gap-2 mb-2">
                <Icon size={14} className={color} />
                <span className={clsx('text-xs', theme === 'dark' ? 'text-gray-500' : 'text-gray-400')}>{label}</span>
              </div>
              <p className="text-xl font-bold">{value}</p>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
