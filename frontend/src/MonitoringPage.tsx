import React, { useEffect, useState } from "react";
import { PieChart, Pie, Cell, ResponsiveContainer, Legend, Tooltip } from "recharts";
import type {
  MonitoredRequestDetail,
  MonitoredRequestSummary,
  GetMonitoredRequestsParams,
  MonitoringStats
} from "./api";
import { getMonitoredRequest, getMonitoredRequests, getMonitoringStats } from "./api";
import AdminSessionBanner from "./AdminSessionBanner";
import { useAdminSession } from "./AdminSessionContext";

const initialParams: GetMonitoredRequestsParams = {
  take: 100
};

const STATUS_COLORS: Record<number, string> = {
  200: "#22c55e",
  201: "#16a34a",
  204: "#15803d",
  400: "#f59e0b",
  401: "#d97706",
  403: "#b45309",
  404: "#92400e",
  500: "#dc2626",
  502: "#b91c1c",
  503: "#991b1b"
};

function statusColorHex(statusCode: number): string {
  if (STATUS_COLORS[statusCode]) return STATUS_COLORS[statusCode];
  if (statusCode >= 500) return "#dc2626";
  if (statusCode >= 400) return "#f59e0b";
  if (statusCode >= 200) return "#22c55e";
  return "#64748b";
}

const MonitoringPage: React.FC = () => {
  const { hasSession } = useAdminSession();
  const [params] = useState<GetMonitoredRequestsParams>(initialParams);
  const [items, setItems] = useState<MonitoredRequestSummary[]>([]);
  const [stats, setStats] = useState<MonitoringStats | null>(null);
  const [details, setDetails] = useState<Record<number, MonitoredRequestDetail | null>>({});
  const [expandedId, setExpandedId] = useState<number | null>(null);
  const [loading, setLoading] = useState(false);
  const [loadingDetailId, setLoadingDetailId] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [hasSession]);

  async function load() {
    try {
      setLoading(true);
      setError(null);
      const [data, statsData] = await Promise.all([
        getMonitoredRequests(params),
        getMonitoringStats()
      ]);
      setItems(data);
      setStats(statsData);
    } catch (err) {
      console.error(err);
      setError("Unable to load monitored requests. Use Admin access if the server requires an admin key.");
    } finally {
      setLoading(false);
    }
  }

  async function handleRowClick(id: number) {
    setExpandedId((current) => (current === id ? null : id));

    if (details[id]) {
      return;
    }

    try {
      setLoadingDetailId(id);
      const detail = await getMonitoredRequest(id);
      setDetails((prev) => ({ ...prev, [id]: detail }));
    } catch (err) {
      console.error(err);
      setError("Unable to load request details.");
    } finally {
      setLoadingDetailId(null);
    }
  }

  function formatTimestamp(value: string) {
    const d = new Date(value);
    if (Number.isNaN(d.getTime())) return value;
    return d.toLocaleString();
  }

  function statusColor(statusCode: number) {
    if (statusCode >= 500) return "text-red-700 bg-red-50 border-red-200";
    if (statusCode >= 400) return "text-amber-700 bg-amber-50 border-amber-200";
    if (statusCode >= 200) return "text-emerald-700 bg-emerald-50 border-emerald-200";
    return "text-slate-700 bg-slate-50 border-slate-200";
  }

  return (
    <div className="space-y-4">
      <AdminSessionBanner />

      <section className="space-y-3">
        <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 className="text-sm font-medium text-slate-700">Request monitoring</h2>
            <p className="text-xs text-slate-500">
              View external API requests received by the Mock Health System. Requests originating from
              the UI are excluded.
            </p>
          </div>
          <div className="flex gap-2">
            <button
              type="button"
              className="inline-flex items-center justify-center rounded-md border border-slate-300 px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50"
              onClick={() => void load()}
              disabled={loading}
            >
              {loading ? "Refreshing..." : "Refresh"}
            </button>
          </div>
        </div>
      </section>

      {error && <div className="text-sm text-red-600">{error}</div>}

      {stats && (
        <section className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
          <h3 className="mb-3 text-sm font-semibold text-slate-800">
            Overview (last 200 requests)
          </h3>
          <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
            <div className="min-h-[280px] overflow-visible">
              <p className="mb-2 text-xs font-medium text-slate-500">Status breakdown</p>
              {stats.statusBreakdown.length > 0 ? (
                <ResponsiveContainer width="100%" height={280} className="overflow-visible">
                  <PieChart margin={{ top: 20, right: 20, bottom: 20, left: 20 }}>
                    <Pie
                      data={stats.statusBreakdown.map((s) => ({
                        name: String(s.statusCode),
                        value: s.count
                      }))}
                      cx="50%"
                      cy="50%"
                      innerRadius={50}
                      outerRadius={70}
                      paddingAngle={2}
                      dataKey="value"
                      label={({ name, percent }) =>
                        `${name} (${(percent * 100).toFixed(0)}%)`
                      }
                    >
                      {stats.statusBreakdown.map((s) => (
                        <Cell key={s.statusCode} fill={statusColorHex(s.statusCode)} />
                      ))}
                    </Pie>
                    <Tooltip formatter={(value: number) => [value, "Count"]} />
                    <Legend />
                  </PieChart>
                </ResponsiveContainer>
              ) : (
                <div className="flex h-[280px] items-center justify-center text-sm text-slate-400">
                  No data yet
                </div>
              )}
            </div>
            <div className="flex flex-col gap-3">
              <div className="rounded-lg border border-slate-200 bg-slate-50/50 p-4">
                <p className="text-xs font-medium text-slate-500">Avg duration</p>
                <p className="mt-1 text-xl font-semibold text-slate-800 tabular-nums">
                  {stats.averageDurationMs != null
                    ? `${stats.averageDurationMs.toFixed(1)} ms`
                    : "—"}
                </p>
              </div>
              <div className="rounded-lg border border-slate-200 bg-slate-50/50 p-4">
                <p className="text-xs font-medium text-slate-500">95th percentile</p>
                <p className="mt-1 text-xl font-semibold text-slate-800 tabular-nums">
                  {stats.percentile95DurationMs != null
                    ? `${stats.percentile95DurationMs} ms`
                    : "—"}
                </p>
              </div>
              <div className="rounded-lg border border-slate-200 bg-slate-50/50 p-4">
                <p className="text-xs font-medium text-slate-500">Max duration</p>
                <p className="mt-1 text-xl font-semibold text-slate-800 tabular-nums">
                  {stats.maxDurationMs != null ? `${stats.maxDurationMs} ms` : "—"}
                </p>
              </div>
            </div>
          </div>
        </section>
      )}

      <section>
        <div className="overflow-hidden rounded-md border border-slate-200">
          <table className="min-w-full divide-y divide-slate-200 text-sm">
            <thead className="bg-slate-50">
              <tr>
                <th className="px-3 py-2 text-left font-medium text-slate-700">Time</th>
                <th className="px-3 py-2 text-left font-medium text-slate-700">Method</th>
                <th className="px-3 py-2 text-left font-medium text-slate-700">Path</th>
                <th className="px-3 py-2 text-left font-medium text-slate-700">Status</th>
                <th className="px-3 py-2 text-left font-medium text-slate-700">Duration</th>
                <th className="px-3 py-2 text-left font-medium text-slate-700">Origin</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 bg-white">
              {items.map((item) => {
                const isExpanded = expandedId === item.id;
                const detail = details[item.id];
                return (
                  <React.Fragment key={item.id}>
                    <tr
                      className="cursor-pointer hover:bg-slate-50"
                      onClick={() => void handleRowClick(item.id)}
                    >
                      <td className="px-3 py-2 align-top text-slate-700">
                        {formatTimestamp(item.createdAtUtc)}
                      </td>
                      <td className="px-3 py-2 align-top font-mono text-xs text-slate-800">
                        {item.method}
                      </td>
                      <td className="px-3 py-2 align-top text-slate-800 break-all">{item.path}</td>
                      <td className="px-3 py-2 align-top">
                        <span
                          className={`inline-flex items-center rounded-full border px-2 py-0.5 text-xs font-medium ${statusColor(
                            item.statusCode
                          )}`}
                        >
                          {item.statusCode}
                        </span>
                      </td>
                      <td className="px-3 py-2 align-top text-slate-700">{item.durationMs} ms</td>
                      <td className="px-3 py-2 align-top text-slate-500 break-all">
                        {item.origin ?? "—"}
                      </td>
                    </tr>
                    {isExpanded && (
                      <tr>
                        <td className="px-3 py-3 bg-slate-50" colSpan={6}>
                          {loadingDetailId === item.id && (
                            <div className="text-xs text-slate-600">Loading details…</div>
                          )}
                          {loadingDetailId !== item.id && detail && (
                            <div className="space-y-2 text-xs text-slate-700">
                              <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
                                <div>
                                  <div className="font-semibold text-slate-800">Request</div>
                                  <div>Path: {detail.path}</div>
                                  {detail.queryString && <div>Query: {detail.queryString}</div>}
                                  {detail.origin && <div>Origin: {detail.origin}</div>}
                                  {detail.referer && <div>Referer: {detail.referer}</div>}
                                  {detail.userAgent && <div>User-Agent: {detail.userAgent}</div>}
                                  {detail.remoteIp && <div>Remote IP: {detail.remoteIp}</div>}
                                </div>
                                <div>
                                  <div className="font-semibold text-slate-800">Response</div>
                                  <div>Status: {detail.statusCode}</div>
                                  <div>Duration: {detail.durationMs} ms</div>
                                  {detail.correlationId && (
                                    <div>Correlation ID: {detail.correlationId}</div>
                                  )}
                                </div>
                              </div>
                              {detail.requestBody && (
                                <div className="space-y-1">
                                  <div className="font-semibold text-slate-800">Request body</div>
                                  <pre className="max-h-48 overflow-auto rounded bg-slate-900 text-slate-50 p-2 text-[11px] whitespace-pre-wrap">
                                    {detail.requestBody}
                                  </pre>
                                </div>
                              )}
                              {detail.responseBody && (
                                <div className="space-y-1">
                                  <div className="font-semibold text-slate-800">Response body</div>
                                  <pre className="max-h-48 overflow-auto rounded bg-slate-900 text-slate-50 p-2 text-[11px] whitespace-pre-wrap">
                                    {detail.responseBody}
                                  </pre>
                                </div>
                              )}
                            </div>
                          )}
                        </td>
                      </tr>
                    )}
                  </React.Fragment>
                );
              })}
              {items.length === 0 && !loading && (
                <tr>
                  <td className="px-3 py-4 text-center text-xs text-slate-500" colSpan={6}>
                    No monitored requests yet. Trigger some API calls from an external client (e.g.
                    curl or Postman) and refresh.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
};

export default MonitoringPage;

