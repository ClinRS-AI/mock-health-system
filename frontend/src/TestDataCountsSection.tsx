import React, { useEffect, useState } from "react";
import { PieChart, Pie, Cell, ResponsiveContainer, Legend, Tooltip } from "recharts";
import {
  getPatientTestDataStats,
  getStudyTestDataStats,
  type PatientTestDataStats,
  type StudyTestDataStats
} from "./api";
import { useAdminSession } from "./AdminSessionContext";
import { DEMO_TEST_DATA_STATS, DEMO_STUDY_TEST_DATA_STATS } from "./demoData";

const CHART_COLORS = ["#0284c7", "#059669", "#d97706", "#7c3aed", "#db2777", "#0891b2", "#65a30d", "#dc2626"];

function chartColor(index: number): string {
  return CHART_COLORS[index % CHART_COLORS.length];
}

interface CategoryPieChartProps {
  data: { name: string; value: number }[];
  valueLabel: string;
  emptyLabel: string;
}

function CategoryPieChart({ data, valueLabel, emptyLabel }: CategoryPieChartProps) {
  if (data.length === 0) {
    return <p className="text-xs text-slate-400">{emptyLabel}</p>;
  }
  return (
    <ResponsiveContainer width="100%" height={220}>
      <PieChart margin={{ top: 10, right: 10, bottom: 10, left: 10 }}>
        <Pie data={data} cx="50%" cy="50%" innerRadius={45} outerRadius={65} paddingAngle={2} dataKey="value">
          {data.map((d, index) => (
            <Cell key={d.name} fill={chartColor(index)} />
          ))}
        </Pie>
        <Tooltip formatter={(value: number) => [value, valueLabel]} />
        <Legend />
      </PieChart>
    </ResponsiveContainer>
  );
}

const TestDataCountsSection: React.FC = () => {
  const { hasSession, isDemoMode, isProbeSettled } = useAdminSession();

  const [stats, setStats] = useState<PatientTestDataStats | null>(null);
  const [studiesStats, setStudiesStats] = useState<StudyTestDataStats | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loadingStats, setLoadingStats] = useState(false);

  async function loadStats() {
    try {
      const data = await getPatientTestDataStats();
      setStats(data);
    } catch (err) {
      console.error(err);
      setStats(null);
      setError("Unable to load patient stats. Check the admin key and backend.");
    }
  }

  async function loadStudiesStats() {
    try {
      const data = await getStudyTestDataStats();
      setStudiesStats(data);
    } catch (err) {
      console.error(err);
      setStudiesStats(null);
      setError("Unable to load study stats. Check the admin key and backend.");
    }
  }

  async function refreshStats() {
    if (isDemoMode) return;
    setLoadingStats(true);
    setError(null);
    await Promise.all([loadStats(), loadStudiesStats()]);
    setLoadingStats(false);
  }

  useEffect(() => {
    if (!hasSession && !isProbeSettled) return;
    if (isDemoMode) {
      setStats(DEMO_TEST_DATA_STATS);
      setStudiesStats(DEMO_STUDY_TEST_DATA_STATS);
    } else {
      setError(null);
      void loadStats();
      void loadStudiesStats();
    }
  }, [hasSession, isDemoMode, isProbeSettled]);

  return (
    <div className="space-y-4">
      <section className="space-y-3">
        <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-3">
          <div>
            <h2 className="text-sm font-medium text-slate-700">Data Counts and Visualizations</h2>
            <p className="text-xs text-slate-500">
              Current volume of synthetic patient and study data in this environment.
            </p>
          </div>
          <button
            type="button"
            onClick={() => void refreshStats()}
            disabled={loadingStats}
            className="inline-flex items-center justify-center rounded-md border border-slate-300 px-3 py-2 text-xs font-medium text-slate-700 hover:bg-slate-50 self-start disabled:opacity-70"
          >
            {loadingStats ? "Refreshing…" : "Refresh stats"}
          </button>
        </div>
      </section>

      {error && <div className="text-sm text-red-600">{error}</div>}

      {stats && (
        <section className="grid gap-4 md:grid-cols-4">
          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <p className="text-xs font-medium text-slate-500">Patient count</p>
            <p className="mt-1 text-xl font-semibold text-slate-800 tabular-nums">
              {stats.patientCount}
            </p>
          </div>
          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <p className="text-xs font-medium text-slate-500">Duplicate patients</p>
            <p className="mt-1 text-xl font-semibold text-slate-800 tabular-nums">
              {stats.duplicatePatientCount}
            </p>
          </div>
          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <p className="text-xs font-medium text-slate-500">Recent audit events (last 5 min)</p>
            <p className="mt-1 text-xl font-semibold text-slate-800 tabular-nums">
              {stats.recentAuditEventCount}
            </p>
          </div>
          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <p className="text-xs font-medium text-slate-500">Total staff</p>
            <p className="mt-1 text-xl font-semibold text-slate-800 tabular-nums">
              {stats.totalStaffCount}
            </p>
          </div>
        </section>
      )}

      {stats && (
        <section className="rounded-lg border border-slate-200 bg-white p-4">
          <p className="mb-2 text-xs font-medium text-slate-500">Patients by site</p>
          <CategoryPieChart
            data={stats.patientsBySite.map((s) => ({ name: s.siteName, value: s.count }))}
            valueLabel="Patients"
            emptyLabel="No patients yet."
          />
        </section>
      )}

      {studiesStats && (
        <section className="grid gap-4 md:grid-cols-3">
          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <p className="text-xs font-medium text-slate-500">Study count</p>
            <p className="mt-1 text-xl font-semibold text-slate-800 tabular-nums">
              {studiesStats.studyCount}
            </p>
          </div>
          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <p className="text-xs font-medium text-slate-500">Arms</p>
            <p className="mt-1 text-xl font-semibold text-slate-800 tabular-nums">
              {studiesStats.armCount}
            </p>
          </div>
          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <p className="text-xs font-medium text-slate-500">Visits</p>
            <p className="mt-1 text-xl font-semibold text-slate-800 tabular-nums">
              {studiesStats.visitCount}
            </p>
          </div>
        </section>
      )}

      {studiesStats && (
        <section className="rounded-lg border border-slate-200 bg-white p-4">
          <p className="mb-2 text-xs font-medium text-slate-500">Studies by status</p>
          <CategoryPieChart
            data={studiesStats.studiesByStatus.map((s) => ({ name: s.statusName, value: s.count }))}
            valueLabel="Studies"
            emptyLabel="No studies yet."
          />
        </section>
      )}
    </div>
  );
};

export default TestDataCountsSection;
