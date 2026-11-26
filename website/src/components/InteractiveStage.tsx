"use client";

import React, { useState, useRef } from "react";
import SyncSoundLogo from "./SyncSoundLogo";

interface SimDevice {
  id: string;
  name: string;
  category: "Earbuds" | "Headphones" | "Wired";
  transport: string;
  selected: boolean;
  volume: number;
  battery?: number;
  isDefault?: boolean;
}

const INITIAL_DEVICES: SimDevice[] = [
  {
    id: "buds",
    name: "OnePlus Nord Buds 3 Pro",
    category: "Earbuds",
    transport: "Bluetooth",
    selected: true,
    volume: 0.92,
    battery: 85,
  },
  {
    id: "sony",
    name: "Sony WH-1000XM5",
    category: "Headphones",
    transport: "Bluetooth",
    selected: true,
    volume: 0.78,
    battery: 72,
    isDefault: true,
  },
  {
    id: "wired",
    name: "Realtek Audio (Wired / Speakers)",
    category: "Wired",
    transport: "Wired / System Audio",
    selected: false,
    volume: 0.74,
  },
];

function DeviceIcon({ category, active }: { category: SimDevice["category"]; active: boolean }) {
  const stroke = active ? "#C65D32" : "currentColor";
  if (category === "Earbuds") {
    return (
      <svg className="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke={stroke} strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
        <path d="M7 6a4 4 0 0 0-1 7.86V19a1 1 0 1 0 2 0V10c0-2.21-.45-4-1-4ZM17 6a4 4 0 0 1 1 7.86V19a1 1 0 1 1-2 0V10c0-2.21.45-4 1-4Z" />
      </svg>
    );
  }
  if (category === "Wired") {
    return (
      <svg className="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke={stroke} strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
        <circle cx="8" cy="8" r="3" />
        <circle cx="16" cy="8" r="3" />
        <path d="M8 11v3a4 4 0 0 0 8 0v-3M12 18v3" />
      </svg>
    );
  }
  return (
    <svg className="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke={stroke} strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <path d="M4 13v-1a8 8 0 0 1 16 0v1M4 13h2.5A1.5 1.5 0 0 1 8 14.5v4A1.5 1.5 0 0 1 6.5 20H5a2 2 0 0 1-2-2v-4a1 1 0 0 1 1-1Zm16 0h-2.5a1.5 1.5 0 0 0-1.5 1.5v4a1.5 1.5 0 0 0 1.5 1.5H19a2 2 0 0 0 2-2v-4a1 1 0 0 0-1-1Z" />
    </svg>
  );
}

export default function InteractiveStage() {
  const [devices, setDevices] = useState<SimDevice[]>(INITIAL_DEVICES);
  const [activeTab, setActiveTab] = useState<"listen" | "headphones">("listen");
  const [isListening, setIsListening] = useState(false);
  const [isTestingSound, setIsTestingSound] = useState(false);
  const [masterVol, setMasterVol] = useState(0.88);
  const [isMuted, setIsMuted] = useState(false);
  const [previewDark, setPreviewDark] = useState(false);
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [expandedDeviceId, setExpandedDeviceId] = useState<string | null>(null);

  const selectedDevices = devices.filter((d) => d.selected);

  const toggleDevice = (id: string) => {
    setDevices((prev) =>
      prev.map((d) => (d.id === id ? { ...d, selected: !d.selected } : d))
    );
  };

  const setDeviceVolume = (id: string, volume: number) => {
    setDevices((prev) =>
      prev.map((d) => (d.id === id ? { ...d, volume } : d))
    );
  };

  const handlePrimaryAction = () => {
    if (isListening) {
      setIsListening(false);
      return;
    }
    if (selectedDevices.length < 2) {
      setActiveTab("headphones");
      return;
    }
    setIsListening(true);
  };

  const triggerTestSound = () => {
    if (selectedDevices.length === 0) return;
    setIsTestingSound(true);
    setTimeout(() => setIsTestingSound(false), 2200);
  };

  const copy = (() => {
    if (isListening && isMuted) {
      return {
        headline: "Session muted.",
        accent: "Everyone's on pause.",
        sub: "Tap Unmute or raise the Sync Dial when you're ready.",
        cta: "Stop listening",
      };
    }
    if (isListening) {
      return {
        headline: "You're all listening.",
        accent: "Everyone's in.",
        sub: `${selectedDevices.length} outputs sharing your Windows sound.`,
        cta: "Stop listening",
      };
    }
    if (selectedDevices.length < 2) {
      return {
        headline: "One's ready.",
        accent: "Add another output.",
        sub: "Combine Bluetooth TWS, 3.5 mm wired headphones, or USB headsets together.",
        cta: "Choose headphones",
      };
    }
    return {
      headline: "Everyone's here.",
      accent: "Listen together.",
      sub: `${selectedDevices.length} outputs in your group. Press Start listening to play.`,
      cta: "Start listening",
    };
  })();

  const dialRef = useRef<HTMLDivElement>(null);
  const startDialDrag = (e: React.PointerEvent) => {
    e.currentTarget.setPointerCapture(e.pointerId);
    const startY = e.clientY;
    const startVal = masterVol;
    const onMove = (ev: PointerEvent) => {
      const next = Math.min(1, Math.max(0, startVal + (startY - ev.clientY) / 110));
      setMasterVol(next);
      setIsMuted(next <= 0.01);
    };
    const onUp = () => {
      window.removeEventListener("pointermove", onMove);
      window.removeEventListener("pointerup", onUp);
    };
    window.addEventListener("pointermove", onMove);
    window.addEventListener("pointerup", onUp);
  };

  const shellBg = previewDark
    ? "bg-[#191817] border-[#36322D] text-[#F4EFE6]"
    : "bg-[#F4EFE6] border-[#DDD5C5] text-[#191817]";
  const railBg = previewDark
    ? "bg-[#131211] border-[#36322D]"
    : "bg-[#ECE5D8] border-[#DDD5C5]";
  const cardBg = previewDark
    ? "bg-[#23201E] border-[#36322D]"
    : "bg-[#FCFBF8] border-[#DDD5C5]";
  const subtleBg = previewDark ? "bg-[#191817]" : "bg-[#E6DEC9]/65";
  const mutedText = previewDark ? "text-[#BDB5A9]" : "text-[#57524C]";

  const effectiveVol = isMuted ? 0 : masterVol;
  const dialAngle = -135 + effectiveVol * 270;

  return (
    <div className="relative">
      <div className="flex flex-wrap items-center justify-between gap-2 mb-3 px-1">
        <span className="font-mono-tech text-[11px] uppercase tracking-[0.14em] text-[#837C73]">
          Interactive Preview · Simulated Windows UI
        </span>
        <div className="flex items-center gap-2">
          <button
            onClick={() => setPreviewDark((d) => !d)}
            className="font-mono-tech text-[11px] px-3 py-1 rounded-full border border-[#DDD5C5] bg-[#FCFBF8] text-[#191817] hover:border-[#C65D32]/60 transition-colors cursor-pointer"
          >
            {previewDark ? "Switch to Light (Cream)" : "Preview Dark Mode"}
          </button>
          <button
            onClick={() => setSidebarOpen(true)}
            className="font-mono-tech text-[11px] px-3 py-1 rounded-full bg-[#191817] text-[#F4EFE6] hover:bg-[#C65D32] transition-colors cursor-pointer"
          >
            Try Quick Sidebar (Ctrl+Shift+S)
          </button>
        </div>
      </div>

      <div className={`relative rounded-[26px] border overflow-hidden shadow-[0_30px_76px_-18px_rgba(25,24,23,0.24)] transition-colors duration-300 ${shellBg}`}>
        <div className="grid md:grid-cols-[204px_1fr]">
          {/* Left Rail */}
          <aside className={`border-b md:border-b-0 md:border-r p-4 flex flex-col justify-between ${railBg}`}>
            <div className="space-y-6">
              <div className="flex items-center gap-2.5 px-1">
                <SyncSoundLogo className="w-8 h-8 rounded-lg" />
                <div>
                  <div className="text-xs font-extrabold tracking-tight">SYNC SOUND</div>
                  <div className={`font-serif-editorial italic text-[11px] ${mutedText}`}>
                    Everyone listening.
                  </div>
                </div>
              </div>

              <nav className="flex md:flex-col gap-1.5">
                <button
                  onClick={() => setActiveTab("listen")}
                  className={`flex-1 md:flex-none flex items-center gap-2.5 px-3 py-2 rounded-xl text-xs font-semibold transition-all cursor-pointer ${
                    activeTab === "listen" ? `${cardBg} border shadow-xs` : `${mutedText} hover:opacity-85`
                  }`}
                >
                  <span className={`w-1 h-4 rounded-full ${activeTab === "listen" ? "bg-[#C65D32]" : "bg-transparent"}`} />
                  <span>Listen</span>
                </button>
                <button
                  onClick={() => setActiveTab("headphones")}
                  className={`flex-1 md:flex-none flex items-center gap-2.5 px-3 py-2 rounded-xl text-xs font-semibold transition-all cursor-pointer ${
                    activeTab === "headphones" ? `${cardBg} border shadow-xs` : `${mutedText} hover:opacity-85`
                  }`}
                >
                  <span className={`w-1 h-4 rounded-full ${activeTab === "headphones" ? "bg-[#C65D32]" : "bg-transparent"}`} />
                  <span>Headphones</span>
                </button>
              </nav>
            </div>

            <div className={`hidden md:block rounded-xl border p-3 ${cardBg}`}>
              <div className="flex items-center gap-2">
                <span className={`w-2 h-2 rounded-full ${isListening ? "bg-[#C65D32]" : "bg-[#297553]"}`} />
                <span className="text-xs font-semibold">{devices.length} connected</span>
              </div>
              <div className={`text-[11px] mt-0.5 pl-4 ${mutedText}`}>
                {selectedDevices.length} in group
              </div>
            </div>
          </aside>

          {/* Main Stage */}
          <div className="p-6 md:p-8 relative">
            {activeTab === "listen" ? (
              <div className="grid lg:grid-cols-[1.15fr_0.85fr] gap-5 items-center">
                {/* Listening Theatre */}
                <div className={`rounded-[22px] border p-6 space-y-5 ${cardBg}`}>
                  <div className="flex flex-wrap items-center gap-2">
                    <span className={`text-[11px] font-semibold px-2.5 py-1 rounded-full border ${previewDark ? "border-[#36322D] bg-[#191817]" : "border-[#DDD5C5] bg-[#ECE5D8]"}`}>
                      {devices.length} connected
                    </span>
                    <span className="text-[11px] font-semibold px-2.5 py-1 rounded-full bg-[#C65D32]/15 border border-[#C65D32]/40 text-[#C65D32]">
                      {selectedDevices.length} in group
                    </span>
                    {isListening && (
                      <span className="text-[11px] font-bold px-2.5 py-1 rounded-full bg-[#C65D32] text-[#FCFBF8]">
                        Live
                      </span>
                    )}
                  </div>

                  {/* 10-Layer Crystal Sync Orb + Orbital Nodes */}
                  <div className="relative w-32 h-32 flex items-center justify-center">
                    <div className="absolute inset-0 rounded-full bg-[#C65D32]/10 border border-[#C65D32]/30 animate-sync-breathe" />
                    <div className={`absolute inset-2 rounded-full border border-dashed ${previewDark ? "border-[#F4EFE6]/15" : "border-[#191817]/15"} ${isListening && !isMuted ? "animate-sync-orbit" : ""}`} />
                    {isListening && !isMuted && (
                      <div className="absolute inset-3 rounded-full border border-[#C65D32] animate-sync-signal" />
                    )}
                    {/* Orbital Nodes */}
                    {selectedDevices.map((d, i) => {
                      const angle = (-90 + i * (360 / Math.max(1, selectedDevices.length))) * (Math.PI / 180);
                      const r = 56;
                      const x = 64 + r * Math.cos(angle);
                      const y = 64 + r * Math.sin(angle);
                      return (
                        <span
                          key={d.id}
                          title={d.name}
                          style={{ left: `${x - 5}px`, top: `${y - 5}px` }}
                          className={`absolute w-2.5 h-2.5 rounded-full border border-white transition-colors ${
                            isListening ? "bg-[#C65D32]" : "bg-[#297553]"
                          }`}
                        />
                      );
                    })}
                    <div className="relative w-20 h-20 rounded-full bg-gradient-to-b from-[#3B3733] via-[#221F1D] to-[#121110] border border-[#F4EFE6]/25 shadow-lg flex items-center justify-center gap-1 overflow-hidden">
                      <div className="absolute -top-6 left-2 w-16 h-10 rounded-full bg-white/15 blur-[2px] pointer-events-none" />
                      <span className={`w-1 h-2.5 rounded-full bg-[#C65D32] ${isListening && !isMuted ? "animate-sync-eq-1" : ""}`} />
                      <span className={`w-1 h-4 rounded-full bg-[#D56A3E] ${isListening && !isMuted ? "animate-sync-eq-2" : ""}`} />
                      <span className={`w-1 h-6 rounded-full bg-[#F4EFE6] ${isListening && !isMuted ? "animate-sync-eq-3" : ""}`} />
                      <span className={`w-1 h-4 rounded-full bg-[#D56A3E] ${isListening && !isMuted ? "animate-sync-eq-4" : ""}`} />
                      <span className={`w-1 h-2.5 rounded-full bg-[#C65D32] ${isListening && !isMuted ? "animate-sync-eq-5" : ""}`} />
                    </div>
                  </div>

                  <div>
                    <h3 className="text-2xl sm:text-3xl font-bold tracking-tight">
                      {copy.headline}
                    </h3>
                    <div className="font-serif-editorial italic text-lg text-[#C65D32] mt-0.5">
                      {copy.accent}
                    </div>
                    <p className={`text-xs sm:text-sm mt-2 ${mutedText}`}>{copy.sub}</p>
                  </div>

                  <div className="flex flex-wrap gap-2.5">
                    <button
                      onClick={handlePrimaryAction}
                      className="px-6 py-2.5 rounded-full text-xs sm:text-sm font-semibold text-[#FCFBF8] bg-[#C65D32] hover:bg-[#D56A3E] shadow-[0_10px_24px_-6px_rgba(198,93,50,0.45)] transition-all active:scale-[0.97] cursor-pointer"
                    >
                      {copy.cta}
                    </button>
                    <button
                      onClick={triggerTestSound}
                      className={`px-4 py-2.5 rounded-full text-xs sm:text-sm font-semibold border transition-colors cursor-pointer ${cardBg}`}
                    >
                      {isTestingSound ? "Testing chime…" : "Test sound"}
                    </button>
                    <button
                      onClick={() => setActiveTab("headphones")}
                      className={`px-4 py-2.5 rounded-full text-xs sm:text-sm font-semibold border transition-colors cursor-pointer ${cardBg}`}
                    >
                      Headphones
                    </button>
                  </div>

                  {isTestingSound && (
                    <div className={`text-xs px-3 py-2 rounded-xl border font-semibold text-[#C65D32] ${subtleBg} border-[#C65D32]/30`}>
                      Simulated chime playing across {selectedDevices.length} selected outputs…
                    </div>
                  )}
                </div>

                {/* Right Column */}
                <div className="space-y-4">
                  {/* Sync Dial */}
                  <div className={`rounded-[20px] border p-4 flex items-center gap-4 ${cardBg}`}>
                    <div
                      ref={dialRef}
                      onPointerDown={startDialDrag}
                      className="relative w-24 h-24 shrink-0 flex items-center justify-center cursor-ns-resize select-none"
                      title="Drag up/down to adjust listening volume"
                    >
                      <svg viewBox="0 0 100 100" className="w-full h-full">
                        <circle cx="50" cy="50" r="40" fill="none" stroke={previewDark ? "#36322D" : "#DAD2C3"} strokeWidth="4" strokeDasharray="188.5 251.3" strokeLinecap="round" transform="rotate(135 50 50)" />
                        <circle cx="50" cy="50" r="40" fill="none" stroke="#C65D32" strokeWidth="4.5" strokeDasharray={`${effectiveVol * 188.5} 251.3`} strokeLinecap="round" transform="rotate(135 50 50)" />
                      </svg>
                      <div className={`absolute w-16 h-16 rounded-full border shadow-md flex flex-col items-center justify-center ${previewDark ? "bg-gradient-to-b from-[#36322E] to-[#161413] border-[#36322D]" : "bg-gradient-to-b from-white to-[#E1D9CA] border-[#DDD5C5]"}`}>
                        <div className="absolute inset-0 flex justify-center pt-1.5 pointer-events-none" style={{ transform: `rotate(${dialAngle}deg)` }}>
                          <span className="w-0.5 h-2.5 rounded-full bg-[#C65D32]" />
                        </div>
                        <span className="font-mono-tech text-xs font-bold tabular-nums-tech">
                          {isMuted ? "MUTED" : `${Math.round(effectiveVol * 100)}%`}
                        </span>
                      </div>
                    </div>

                    <div className="space-y-2">
                      <div className="text-sm font-bold">Listening volume</div>
                      <p className={`text-[11.5px] leading-snug ${mutedText}`}>
                        Drag the Sync Dial or click any device row below for individual trim.
                      </p>
                      <button onClick={() => setIsMuted((m) => !m)} className={`text-[11px] font-semibold px-3 py-1 rounded-full border cursor-pointer ${cardBg}`}>
                        {isMuted ? "Unmute" : "Mute"}
                      </button>
                    </div>
                  </div>

                  {/* Listening Group with Expandable Trim */}
                  <div className={`rounded-[20px] border p-4 space-y-2.5 ${cardBg}`}>
                    <div className="flex items-center justify-between">
                      <span className="text-sm font-bold">Listening group</span>
                      <button onClick={() => setActiveTab("headphones")} className="text-xs font-semibold text-[#C65D32] hover:underline cursor-pointer">
                        Manage
                      </button>
                    </div>

                    {devices.map((d) => {
                      const isExpanded = expandedDeviceId === d.id;
                      return (
                        <div
                          key={d.id}
                          className={`rounded-xl border transition-all ${
                            d.selected ? "border-[#C65D32]/50 bg-[#C65D32]/[0.05]" : `border-transparent ${subtleBg}`
                          }`}
                        >
                          <div className="flex items-center justify-between p-2.5">
                            <button
                              onClick={() => setExpandedDeviceId(isExpanded ? null : d.id)}
                              className="flex items-center gap-2.5 min-w-0 flex-1 text-left cursor-pointer"
                            >
                              <div className={`w-8 h-8 rounded-lg border flex items-center justify-center shrink-0 ${cardBg}`}>
                                <DeviceIcon category={d.category} active={d.selected} />
                              </div>
                              <div className="min-w-0">
                                <div className="text-xs font-semibold truncate">{d.name}</div>
                                <div className={`text-[11px] ${mutedText}`}>
                                  {d.transport}
                                  {d.battery ? ` · ${d.battery}%` : ""}
                                </div>
                              </div>
                            </button>

                            <button
                              onClick={() => toggleDevice(d.id)}
                              className={`w-6 h-6 rounded-full border flex items-center justify-center shrink-0 transition-colors cursor-pointer ${
                                d.selected ? "bg-[#C65D32] border-[#C65D32] text-[#FCFBF8]" : `${cardBg}`
                              }`}
                            >
                              {d.selected && (
                                <svg className="w-3 h-3" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                                  <path d="M5 12.5 9.5 17 19 7.5" strokeLinecap="round" strokeLinejoin="round" />
                                </svg>
                              )}
                            </button>
                          </div>

                          {isExpanded && (
                            <div className="px-3 pb-2.5 pt-1 flex items-center gap-2">
                              <span className={`text-[10px] font-mono-tech ${mutedText}`}>LEVEL</span>
                              <input
                                type="range"
                                min={0}
                                max={100}
                                value={Math.round(d.volume * 100)}
                                onChange={(e) => setDeviceVolume(d.id, Number(e.target.value) / 100)}
                                className="flex-1 accent-[#C65D32] h-1.5 cursor-pointer"
                              />
                              <span className="text-[11px] font-mono-tech font-semibold w-8 text-right">
                                {Math.round(d.volume * 100)}%
                              </span>
                            </div>
                          )}
                        </div>
                      );
                    })}
                  </div>
                </div>
              </div>
            ) : (
              /* Headphones Collection */
              <div className="space-y-4">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div>
                    <h3 className="text-xl font-bold">Your headphones. Your listening group.</h3>
                    <p className={`text-xs ${mutedText}`}>Wired 3.5 mm, USB-C, and Bluetooth TWS run together on one timeline.</p>
                  </div>
                  <button onClick={() => setActiveTab("listen")} className="text-xs font-semibold px-4 py-2 rounded-full bg-[#C65D32] text-[#FCFBF8] cursor-pointer">
                    Back to Listen
                  </button>
                </div>

                <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-3">
                  {devices.map((d) => (
                    <div
                      key={d.id}
                      onClick={() => toggleDevice(d.id)}
                      className={`rounded-[18px] border p-4 space-y-3 cursor-pointer transition-all ${cardBg} ${
                        d.selected ? "border-[#C65D32]/60" : ""
                      }`}
                    >
                      <div className="flex items-start justify-between">
                        <div className={`w-10 h-10 rounded-xl border flex items-center justify-center ${d.selected ? "bg-[#C65D32]/15 border-[#C65D32]/40" : ""}`}>
                          <DeviceIcon category={d.category} active={d.selected} />
                        </div>
                        <span
                          className={`text-[11px] font-semibold px-2.5 py-1 rounded-full ${
                            d.selected
                              ? "bg-[#C65D32] text-[#FCFBF8]"
                              : `${previewDark ? "bg-[#191817] text-[#BDB5A9]" : "bg-[#ECE5D8] text-[#57524C]"}`
                          }`}
                        >
                          {d.selected ? "In group" : "Join"}
                        </span>
                      </div>
                      <div>
                        <div className="text-sm font-bold">{d.name}</div>
                        <div className={`text-xs mt-0.5 ${mutedText}`}>
                          {d.transport}
                          {d.battery ? ` · ${d.battery}%` : ""}
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Simulated Quick Sidebar Overlay */}
      {sidebarOpen && (
        <div
          className="fixed inset-0 z-50 bg-[#191817]/35 backdrop-blur-xs flex justify-end p-4"
          onClick={() => setSidebarOpen(false)}
        >
          <div
            className="w-full max-w-[360px] h-fit my-auto cream-card rounded-[24px] p-4 space-y-3 shadow-2xl text-[#191817]"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <SyncSoundLogo className="w-6 h-6 rounded-md" />
                <div>
                  <div className="text-xs font-extrabold tracking-wide">SYNC SOUND</div>
                  <div className="text-[10px] text-[#837C73]">{selectedDevices.length} of {devices.length} in group</div>
                </div>
              </div>
              <button
                onClick={() => setSidebarOpen(false)}
                className="text-xs font-semibold text-[#837C73] hover:text-[#191817] px-2 py-1 cursor-pointer"
              >
                Close
              </button>
            </div>

            <div className="ink-card rounded-[18px] p-4 text-[#F4EFE6] space-y-3">
              <div>
                <div className="text-base font-bold">{copy.headline}</div>
                <div className="font-serif-editorial italic text-xs text-[#C65D32]">{copy.accent}</div>
              </div>
              <div className="flex gap-2">
                <button
                  onClick={handlePrimaryAction}
                  className="flex-1 py-2.5 rounded-full text-xs font-semibold bg-[#C65D32] hover:bg-[#D56A3E] text-[#FCFBF8] cursor-pointer"
                >
                  {copy.cta}
                </button>
                <button
                  onClick={triggerTestSound}
                  className="px-3 py-2.5 rounded-full text-xs font-semibold bg-white/10 hover:bg-white/15 text-[#F4EFE6] cursor-pointer"
                >
                  Test
                </button>
              </div>
            </div>

            <div className="space-y-1.5">
              {devices.map((d) => (
                <button
                  key={d.id}
                  onClick={() => toggleDevice(d.id)}
                  className={`w-full flex items-center justify-between px-3 py-2 rounded-xl border text-left cursor-pointer ${
                    d.selected ? "border-[#C65D32]/50 bg-[#FCFBF8]" : "border-[#DDD5C5] bg-[#ECE5D8]/60"
                  }`}
                >
                  <span className="text-xs font-semibold truncate">{d.name}</span>
                  <span className="text-[11px] font-mono-tech text-[#C65D32]">
                    {d.selected ? "In group" : "Join"}
                  </span>
                </button>
              ))}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
