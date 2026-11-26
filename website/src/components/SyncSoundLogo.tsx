import React from "react";

interface SyncSoundLogoProps {
  className?: string;
}

/**
 * Pure 8K-crisp vector squircle logo matching the SYNC SOUND hardware aesthetic.
 * Renders at infinite resolution with zero pixelation on any display.
 */
export default function SyncSoundLogo({
  className = "w-8 h-8",
}: SyncSoundLogoProps) {
  return (
    <svg
      viewBox="0 0 64 64"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      className={className}
      aria-label="SYNC SOUND"
    >
      <defs>
        <linearGradient
          id="syncSquircleBg"
          x1="0"
          y1="0"
          x2="0"
          y2="64"
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0%" stopColor="#242429" />
          <stop offset="100%" stopColor="#111114" />
        </linearGradient>
        <linearGradient
          id="syncOrangeBar"
          x1="0"
          y1="14"
          x2="0"
          y2="50"
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0%" stopColor="#F06E51" />
          <stop offset="100%" stopColor="#D94B2D" />
        </linearGradient>
      </defs>

      {/* Tactile Dark Squircle Base */}
      <rect
        x="1"
        y="1"
        width="62"
        height="62"
        rx="16"
        fill="url(#syncSquircleBg)"
        stroke="rgba(255,255,255,0.16)"
        strokeWidth="1.5"
      />

      {/* 5 Crisp Equalizer Bars */}
      <rect x="14.5" y="25" width="4.5" height="14" rx="2.25" fill="url(#syncOrangeBar)" />
      <rect x="22" y="19" width="4.5" height="26" rx="2.25" fill="url(#syncOrangeBar)" />
      <rect x="29.75" y="13" width="4.5" height="38" rx="2.25" fill="#F7F4ED" />
      <rect x="37.5" y="19" width="4.5" height="26" rx="2.25" fill="url(#syncOrangeBar)" />
      <rect x="45" y="25" width="4.5" height="14" rx="2.25" fill="url(#syncOrangeBar)" />
    </svg>
  );
}
