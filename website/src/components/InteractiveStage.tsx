"use client";

import React, { useState, useEffect, useRef } from "react";

interface SimDevice {
  id: string;
  name: string;
  type: string;
  selected: boolean;
  volume: number;
  isDefault?: boolean;
}

const INITIAL_DEVICES: SimDevice[] = [
  {
    id: "1",
    name: "OnePlus Nord Buds 3 Pro",
    type: "Bluetooth · Stereo A2DP",
    selected: true,
    volume: 0.9,
    isDefault: true,
  },
  {
    id: "2",
    name: "AirPods Pro (2nd Gen)",
    type: "Bluetooth · Stereo A2DP",
    selected: true,
    volume: 0.78,
  },
  {
    id: "3",
    name: "Realtek Studio Monitors",
    type: "Wired · 48 kHz",
    selected: false,
    volume: 0.65,
  },
];

function RotaryKnob({
  value,
  onChange,
  size = 44,
}: {
  value: number;
  onChange: (val: number) => void;
  size?: number;
}) {
  const draggingRef = useRef(false);
  const lastYRef = useRef(0);

  const startAngle = -135;
  const sweep = 270;
  const currentAngle = startAngle + value * sweep;

  const r = size / 2 - 4;
  const cx = size / 2;
  const cy = size / 2;

  const polar = (deg: number) => {
    const rad = ((deg - 90) * Math.PI) / 180;
    return {
      x: cx + r * Math.cos(rad),
      y: cy + r * Math.sin(rad),
    };
  };

  const describeArc = (a1: number, a2: number) => {
    const s = polar(a1);
    const e = polar(a2);
    const large = Math.abs(a2 - a1) > 180 ? 1 : 0;
    return `M ${s.x.toFixed(2)} ${s.y.toFixed(2)} A ${r} ${r} 0 ${large} 1 ${e.x.toFixed(2)} ${e.y.toFixed(2)}`;
  };

  const handlePointerDown = (e: React.PointerEvent<HTMLDivElement>) => {
    draggingRef.current = true;
    lastYRef.current = e.clientY;
    e.currentTarget.setPointerCapture(e.pointerId);
  };

  const handlePointerMove = (e: React.PointerEvent<HTMLDivElement>) => {
    if (!draggingRef.current) return;
    const dy = lastYRef.current - e.clientY;
    lastYRef.current = e.clientY;
    const next = Math.min(1, Math.max(0, value + dy / 95));
    onChange(next);
  };

  const handlePointerUp = () => {
    draggingRef.current = false;
  };

  return (
    <div
      className="flex flex-col items-center select-none cursor-ns-resize group"
      title="Drag up or down to adjust device volume. Double-click to reset."
      onPointerDown={handlePointerDown}
      onPointerMove={handlePointerMove}
      onPointerUp={handlePointerUp}
      onDoubleClick={() => onChange(1)}
    >
      <div className="relative" style={{ width: size, height: size }}>
        <svg width={size} height={size} className="overflow-visible">
          <path
            d={describeArc(startAngle, startAngle + sweep)}
            fill="none"
            stroke="rgba(255,255,255,0.14)"
            strokeWidth={3}
            strokeLinecap="round"
          />
          {value > 0.01 && (
            <path
              d={describeArc(startAngle, currentAngle)}
              fill="none"
              stroke="#22D3EE"
              strokeWidth={3}
              strokeLinecap="round"
              style={{
                filter: "drop-shadow(0 0 6px rgba(34,211,238,0.65))",
              }}
            />
          )}
        </svg>
        {/* Brushed Metallic Dial Cap */}
        <div
          className="absolute inset-0 m-auto rounded-full border border-white/15 shadow-lg flex items-center justify-center"
          style={{
            width: size * 0.62,
            height: size * 0.62,
            background: "linear-gradient(180deg, #252E42 0%, #111622 100%)",
            transform: `rotate(${currentAngle}deg)`,
          }}
        >
          <span
            className="w-[2.5px] h-[6px] rounded-full bg-[#22D3EE]"
            style={{ transform: "translateY(-5px)" }}
          />
        </div>
      </div>
      <span className="font-mono-tech text-[10px] text-[#94A3B8] mt-0.5">
        {Math.round(value * 100)}%
      </span>
    </div>
  );
}

export default function InteractiveStage() {
  const [devices, setDevices] = useState<SimDevice[]>(INITIAL_DEVICES);
  const [isSharing, setIsSharing] = useState(true);
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const selectedCount = devices.filter((d) => d.selected).length;

  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.ctrlKey && e.shiftKey && e.key.toLowerCase() === "s") {
        e.preventDefault();
        setSidebarOpen((prev) => !prev);
      }
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, []);

  const toggleDevice = (id: string) => {
    setDevices((prev) =>
      prev.map((d) => (d.id === id ? { ...d, selected: !d.selected } : d))
    );
  };

  const setVolume = (id: string, volume: number) => {
    setDevices((prev) =>
      prev.map((d) => (d.id === id ? { ...d, volume } : d))
    );
  };

  return (
    <div className="relative">
      {/* Main App Window Replica */}
      <div className="glass-card rounded-[24px] overflow-hidden border border-white/10">
        {/* Custom Window Chrome Bar */}
        <div className="flex items-center justify-between px-5 py-3.5 border-b border-white/[0.06] bg-[#06080E]/80">
          <div className="flex items-center gap-3">
            <img
              src="/app-icon.png"
              alt="SYNC SOUND Icon"
              className="w-6 h-6 rounded-md shadow-md"
            />
            <span className="font-display text-xs tracking-wider font-bold text-[#F8FAFC]">
              SYNC SOUND
            </span>
            <span className="font-mono-tech text-[10px] px-2 py-0.5 rounded-full bg-[#22D3EE]/10 text-[#22D3EE] border border-[#22D3EE]/25">
              INTERACTIVE PREVIEW
            </span>
          </div>

          <div className="flex items-center gap-2">
            <button
              onClick={() => setSidebarOpen(true)}
              className="font-mono-tech text-[11px] px-3 py-1.5 rounded-lg bg-white/[0.04] hover:bg-[#22D3EE]/15 text-[#94A3B8] hover:text-[#22D3EE] border border-white/10 transition-colors cursor-pointer"
            >
              Simulate Ctrl+Shift+S
            </button>
          </div>
        </div>

        {/* Two-Column Interior: Orbital Stage + Live Device Knobs */}
        <div className="grid lg:grid-cols-12 gap-6 p-6 md:p-8 items-center">
          {/* Left: Orbital Spatial Acoustic Core */}
          <div className="lg:col-span-5 flex flex-col items-center text-center py-4">
            <div className="relative w-48 h-48 flex items-center justify-center mb-5">
              {/* Outer Dashed Orbit */}
              <div className="absolute inset-0 rounded-full border border-dashed border-[#22D3EE]/25 animate-orbit-cw" />
              {/* Inner Counter Orbit */}
              <div className="absolute inset-4 rounded-full border border-[#22D3EE]/35 border-t-transparent border-l-transparent animate-orbit-ccw" />

              {/* Sonar Pulse when Sharing */}
              {isSharing && selectedCount > 0 && (
                <div className="absolute inset-5 rounded-full border-2 border-[#22D3EE]/70 animate-sonar pointer-events-none" />
              )}

              {/* 3D Specular Glass Sphere */}
              <div
                className="relative w-24 h-24 rounded-full border border-white/15 flex items-center justify-center shadow-2xl"
                style={{
                  background:
                    "radial-gradient(circle at 32% 24%, #384766 0%, #1A2234 48%, #0B0F19 100%)",
                }}
              >
                <div className="flex items-center gap-1 h-9">
                  {[
                    "h-3 animate-eq-1 bg-[#22D3EE]",
                    "h-6 animate-eq-2 bg-[#22D3EE]",
                    "h-8 animate-eq-3 bg-[#F8FAFC]",
                    "h-6 animate-eq-4 bg-[#22D3EE]",
                    "h-3 animate-eq-5 bg-[#22D3EE]",
                  ].map((cls, i) => (
                    <span
                      key={i}
                      className={`w-1 rounded-full origin-center transition-opacity ${
                        isSharing && selectedCount > 0 ? cls : "h-3 bg-[#64748B]/60"
                      }`}
                    />
                  ))}
                </div>
              </div>
            </div>

            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-white/[0.04] border border-white/10 mb-3">
              <span
                className={`w-2 h-2 rounded-full ${
                  isSharing && selectedCount > 0
                    ? "bg-[#34D399] shadow-[0_0_8px_#34D399]"
                    : "bg-[#64748B]"
                }`}
              />
              <span className="text-xs font-medium text-[#F8FAFC]">
                {isSharing && selectedCount > 0
                  ? `You're all listening · ${selectedCount} active`
                  : "Ready to listen"}
              </span>
            </div>

            <button
              onClick={() => setIsSharing((s) => !s)}
              disabled={selectedCount === 0}
              className="mt-2 px-7 py-3 rounded-full font-semibold text-sm text-[#06121A] transition-all cursor-pointer disabled:opacity-40 hover:scale-[1.03] active:scale-[0.97]"
              style={{
                background: "linear-gradient(135deg, #38E8FF 0%, #06B6D4 100%)",
                boxShadow: "0 10px 30px -6px rgba(34,211,238,0.5)",
              }}
            >
              {isSharing ? "Stop sharing" : "Start sharing"}
            </button>
          </div>

          {/* Right: Per-Device Cards with Drag-able Rotary Volume Knobs */}
          <div className="lg:col-span-7 space-y-3">
            <div className="flex items-center justify-between px-1 mb-1">
              <span className="text-xs uppercase tracking-widest text-[#64748B] font-semibold">
                Connected Headphones — Drag knobs to test volume
              </span>
              <span className="font-mono-tech text-xs text-[#22D3EE]">
                {selectedCount} / {devices.length} active
              </span>
            </div>

            {devices.map((d) => (
              <div
                key={d.id}
                className={`rounded-2xl p-4 transition-all flex items-center justify-between gap-4 ${
                  d.selected ? "glass-card-active" : "glass-card opacity-75"
                }`}
              >
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className="font-semibold text-sm text-[#F8FAFC] truncate">
                      {d.name}
                    </span>
                    {d.isDefault && (
                      <span className="text-[10px] font-semibold px-2 py-0.5 rounded-md bg-[#22D3EE]/10 text-[#22D3EE] border border-[#22D3EE]/30">
                        Playing original sound
                      </span>
                    )}
                  </div>
                  <div className="flex items-center gap-1.5 mt-1 text-xs text-[#94A3B8]">
                    <span className="w-1.5 h-1.5 rounded-full bg-[#34D399]" />
                    <span>{d.type}</span>
                  </div>
                </div>

                {/* Live Interactive Rotary Volume Knob */}
                <RotaryKnob
                  value={d.volume}
                  onChange={(v) => setVolume(d.id, v)}
                  size={42}
                />

                {/* Spring Pill Toggle */}
                <button
                  onClick={() => toggleDevice(d.id)}
                  aria-label={`Toggle ${d.name}`}
                  className={`relative w-12 h-6.5 rounded-full transition-colors cursor-pointer shrink-0 border ${
                    d.selected
                      ? "bg-[#22D3EE] border-white/30"
                      : "bg-[#242D42] border-white/10"
                  }`}
                >
                  <span
                    className={`block w-5 h-5 rounded-full transition-transform shadow-md ${
                      d.selected
                        ? "translate-x-6 bg-[#06121A]"
                        : "translate-x-0.5 bg-[#F8FAFC]"
                    }`}
                  />
                </button>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Simulated Quick Sidebar Drawer */}
      {sidebarOpen && (
        <div
          className="fixed inset-0 z-50 bg-black/50 backdrop-blur-sm flex justify-end p-4"
          onClick={() => setSidebarOpen(false)}
        >
          <div
            className="w-full max-w-[360px] h-fit my-auto glass-card-active rounded-[24px] p-5 space-y-4"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2.5">
                <img
                  src="/app-icon.png"
                  alt="SYNC SOUND"
                  className="w-5 h-5 rounded"
                />
                <span className="font-display text-xs font-bold tracking-wider">
                  QUICK SIDEBAR
                </span>
              </div>
              <button
                onClick={() => setSidebarOpen(false)}
                className="text-[#94A3B8] hover:text-white text-lg leading-none px-2 cursor-pointer"
              >
                ×
              </button>
            </div>

            <div className="glass-card rounded-2xl p-4">
              <div className="flex items-center gap-2">
                <span
                  className={`w-2 h-2 rounded-full ${
                    isSharing ? "bg-[#34D399]" : "bg-[#64748B]"
                  }`}
                />
                <span className="font-semibold text-base">
                  {isSharing ? "You're all listening." : "Ready to listen."}
                </span>
              </div>
              <p className="text-xs text-[#94A3B8] mt-1 mb-3">
                {selectedCount} devices selected ·{" "}
                <span className="font-mono-tech text-[#22D3EE]">
                  Ctrl+Shift+S
                </span>{" "}
                anywhere
              </p>
              <button
                onClick={() => setIsSharing((s) => !s)}
                className="w-full py-2.5 rounded-full font-semibold text-xs text-[#06121A] cursor-pointer"
                style={{
                  background: "linear-gradient(135deg, #38E8FF 0%, #06B6D4 100%)",
                }}
              >
                {isSharing ? "Stop sharing" : "Start sharing"}
              </button>
            </div>

            <div className="space-y-2">
              {devices.map((d) => (
                <div
                  key={d.id}
                  className="glass-card rounded-xl p-3 flex items-center justify-between gap-2"
                >
                  <div className="min-w-0 flex-1">
                    <div className="text-xs font-semibold truncate">
                      {d.name}
                    </div>
                    <div className="text-[10px] text-[#64748B]">{d.type}</div>
                  </div>
                  <RotaryKnob
                    value={d.volume}
                    onChange={(v) => setVolume(d.id, v)}
                    size={34}
                  />
                  <button
                    onClick={() => toggleDevice(d.id)}
                    className={`w-10 h-5.5 rounded-full transition-colors cursor-pointer shrink-0 ${
                      d.selected ? "bg-[#22D3EE]" : "bg-[#242D42]"
                    }`}
                  >
                    <span
                      className={`block w-4 h-4 rounded-full transition-transform ${
                        d.selected
                          ? "translate-x-5 bg-[#06121A]"
                          : "translate-x-0.5 bg-white"
                      }`}
                    />
                  </button>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
