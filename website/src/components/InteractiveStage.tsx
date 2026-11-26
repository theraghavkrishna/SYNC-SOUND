"use client";

import React, { useState, useEffect, useRef } from "react";
import SyncSoundLogo from "./SyncSoundLogo";

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
    volume: 0.92,
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
    name: "Realtek Studio Speakers",
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
    onChange(Math.min(1, Math.max(0, value + dy / 95)));
  };

  const handlePointerUp = () => {
    draggingRef.current = false;
  };

  return (
    <div
      className="flex flex-col items-center select-none cursor-ns-resize"
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
            stroke="#DFD9CC"
            strokeWidth={3}
            strokeLinecap="round"
          />
          {value > 0.01 && (
            <path
              d={describeArc(startAngle, currentAngle)}
              fill="none"
              stroke="#E85D3F"
              strokeWidth={3}
              strokeLinecap="round"
              style={{
                filter: "drop-shadow(0 0 5px rgba(232,93,63,0.45))",
              }}
            />
          )}
        </svg>
        {/* Brushed Ink-Black Hardware Dial Cap */}
        <div
          className="absolute inset-0 m-auto rounded-full border border-[#3A3A40] shadow-md flex items-center justify-center"
          style={{
            width: size * 0.62,
            height: size * 0.62,
            background: "linear-gradient(180deg, #2B2B31 0%, #121215 100%)",
            transform: `rotate(${currentAngle}deg)`,
          }}
        >
          <span
            className="w-[2.5px] h-[6px] rounded-full bg-[#E85D3F]"
            style={{ transform: "translateY(-5px)" }}
          />
        </div>
      </div>
      <span className="font-mono-tech text-[10px] text-[#52525B] mt-0.5">
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
      <div className="cream-card rounded-[26px] overflow-hidden">
        {/* Custom Cream & Ink Chrome Bar */}
        <div className="flex items-center justify-between px-5 py-3.5 border-b border-[#E4DFD3] bg-[#EFECE3]/80">
          <div className="flex items-center gap-3">
            <SyncSoundLogo className="w-6 h-6 rounded-md shadow-sm" />
            <span className="text-xs tracking-wider font-bold text-[#141417]">
              SYNC SOUND
            </span>
            <span className="font-mono-tech text-[10px] px-2.5 py-0.5 rounded-full bg-[#E85D3F]/10 text-[#E85D3F] border border-[#E85D3F]/25 font-semibold">
              LIVE DEMO
            </span>
          </div>

          <button
            onClick={() => setSidebarOpen(true)}
            className="font-mono-tech text-[11px] px-3 py-1.5 rounded-lg bg-white hover:bg-[#141417] text-[#141417] hover:text-[#F7F4ED] border border-[#E4DFD3] transition-colors cursor-pointer font-medium"
          >
            Simulate Ctrl+Shift+S
          </button>
        </div>

        {/* Interior: Orbital Stage + Live Device Knobs */}
        <div className="grid lg:grid-cols-12 gap-8 p-6 md:p-9 items-center bg-[#F7F4ED]/60">
          {/* Left: Orbital Spatial Acoustic Core */}
          <div className="lg:col-span-5 flex flex-col items-center text-center py-4">
            <div className="relative w-48 h-48 flex items-center justify-center mb-5">
              {/* Outer Dashed Orbit */}
              <div className="absolute inset-0 rounded-full border border-dashed border-[#E85D3F]/40 animate-orbit-cw" />
              {/* Inner Counter Orbit */}
              <div className="absolute inset-4 rounded-full border border-[#E85D3F]/60 border-t-transparent border-l-transparent animate-orbit-ccw" />

              {/* Sonar Pulse when Sharing */}
              {isSharing && selectedCount > 0 && (
                <div className="absolute inset-5 rounded-full border-2 border-[#E85D3F]/70 animate-sonar pointer-events-none" />
              )}

              {/* 3D Obsidian Hardware Sphere */}
              <div
                className="relative w-24 h-24 rounded-full border border-[#35353B] flex items-center justify-center shadow-2xl"
                style={{
                  background:
                    "radial-gradient(circle at 32% 24%, #35353B 0%, #1C1C21 48%, #0B0B0D 100%)",
                }}
              >
                <div className="flex items-center gap-1 h-9">
                  {[
                    "h-3 animate-eq-1 bg-[#E85D3F]",
                    "h-6 animate-eq-2 bg-[#E85D3F]",
                    "h-8 animate-eq-3 bg-[#F7F4ED]",
                    "h-6 animate-eq-4 bg-[#E85D3F]",
                    "h-3 animate-eq-5 bg-[#E85D3F]",
                  ].map((cls, i) => (
                    <span
                      key={i}
                      className={`w-1 rounded-full origin-center transition-opacity ${
                        isSharing && selectedCount > 0 ? cls : "h-3 bg-[#52525B]"
                      }`}
                    />
                  ))}
                </div>
              </div>
            </div>

            <div className="inline-flex items-center gap-2 px-3.5 py-1 rounded-full bg-white border border-[#E4DFD3] shadow-xs mb-3">
              <span
                className={`w-2 h-2 rounded-full ${
                  isSharing && selectedCount > 0
                    ? "bg-[#15803D]"
                    : "bg-[#84848E]"
                }`}
              />
              <span className="text-xs font-semibold text-[#141417]">
                {isSharing && selectedCount > 0
                  ? `You're all listening · ${selectedCount} active`
                  : "Ready to listen"}
              </span>
            </div>

            <button
              onClick={() => setIsSharing((s) => !s)}
              disabled={selectedCount === 0}
              className="mt-2 px-7 py-3 rounded-full font-semibold text-sm text-white transition-all cursor-pointer disabled:opacity-40 hover:scale-[1.03] active:scale-[0.97]"
              style={{
                background: "linear-gradient(135deg, #F06E51 0%, #D94B2D 100%)",
                boxShadow: "0 12px 28px -6px rgba(232,93,63,0.45)",
              }}
            >
              {isSharing ? "Stop sharing" : "Start sharing"}
            </button>
          </div>

          {/* Right: Per-Device Cards with Drag-able Rotary Volume Knobs */}
          <div className="lg:col-span-7 space-y-3">
            <div className="flex items-center justify-between px-1 mb-1">
              <span className="text-[11px] uppercase tracking-widest text-[#84848E] font-semibold">
                Headphones — Drag knobs to test individual volume
              </span>
              <span className="font-mono-tech text-xs text-[#E85D3F] font-semibold">
                {selectedCount} / {devices.length} active
              </span>
            </div>

            {devices.map((d) => (
              <div
                key={d.id}
                className={`rounded-2xl p-4 transition-all flex items-center justify-between gap-4 ${
                  d.selected ? "cream-card-active" : "cream-card opacity-75"
                }`}
              >
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className="font-semibold text-sm text-[#141417] truncate">
                      {d.name}
                    </span>
                    {d.isDefault && (
                      <span className="text-[10px] font-semibold px-2 py-0.5 rounded-md bg-[#FDF0ED] text-[#E85D3F] border border-[#F4C2B5]">
                        Playing original sound
                      </span>
                    )}
                  </div>
                  <div className="flex items-center gap-1.5 mt-1 text-xs text-[#52525B]">
                    <span className="w-1.5 h-1.5 rounded-full bg-[#15803D]" />
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
                      ? "bg-[#E85D3F] border-[#D54B2E]"
                      : "bg-[#D6D1C6] border-[#C7C1B5]"
                  }`}
                >
                  <span
                    className={`block w-5 h-5 rounded-full bg-white transition-transform shadow-sm ${
                      d.selected ? "translate-x-6" : "translate-x-0.5"
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
          className="fixed inset-0 z-50 bg-[#141417]/35 backdrop-blur-xs flex justify-end p-4"
          onClick={() => setSidebarOpen(false)}
        >
          <div
            className="w-full max-w-[360px] h-fit my-auto cream-card rounded-[24px] p-5 space-y-4 shadow-2xl"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2.5">
                <SyncSoundLogo className="w-5 h-5 rounded" />
                <span className="text-xs font-bold tracking-wider text-[#141417]">
                  QUICK SIDEBAR
                </span>
              </div>
              <button
                onClick={() => setSidebarOpen(false)}
                className="text-[#84848E] hover:text-[#141417] text-lg leading-none px-2 cursor-pointer"
              >
                ×
              </button>
            </div>

            <div className="bg-[#EFECE3] rounded-2xl p-4 border border-[#E4DFD3]">
              <div className="flex items-center gap-2">
                <span
                  className={`w-2 h-2 rounded-full ${
                    isSharing ? "bg-[#15803D]" : "bg-[#84848E]"
                  }`}
                />
                <span className="font-semibold text-base text-[#141417]">
                  {isSharing ? "You're all listening." : "Ready to listen."}
                </span>
              </div>
              <p className="text-xs text-[#52525B] mt-1 mb-3">
                {selectedCount} devices selected ·{" "}
                <span className="font-mono-tech text-[#E85D3F] font-semibold">
                  Ctrl+Shift+S
                </span>{" "}
                anywhere
              </p>
              <button
                onClick={() => setIsSharing((s) => !s)}
                className="w-full py-2.5 rounded-full font-semibold text-xs text-white cursor-pointer"
                style={{
                  background: "linear-gradient(135deg, #F06E51 0%, #D94B2D 100%)",
                }}
              >
                {isSharing ? "Stop sharing" : "Start sharing"}
              </button>
            </div>

            <div className="space-y-2">
              {devices.map((d) => (
                <div
                  key={d.id}
                  className="cream-card rounded-xl p-3 flex items-center justify-between gap-2"
                >
                  <div className="min-w-0 flex-1">
                    <div className="text-xs font-semibold truncate text-[#141417]">
                      {d.name}
                    </div>
                    <div className="text-[10px] text-[#84848E]">{d.type}</div>
                  </div>
                  <RotaryKnob
                    value={d.volume}
                    onChange={(v) => setVolume(d.id, v)}
                    size={34}
                  />
                  <button
                    onClick={() => toggleDevice(d.id)}
                    className={`w-10 h-5.5 rounded-full transition-colors cursor-pointer shrink-0 ${
                      d.selected ? "bg-[#E85D3F]" : "bg-[#D6D1C6]"
                    }`}
                  >
                    <span
                      className={`block w-4 h-4 rounded-full bg-white transition-transform ${
                        d.selected ? "translate-x-5" : "translate-x-0.5"
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
