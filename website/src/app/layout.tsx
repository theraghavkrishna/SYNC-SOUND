import type { Metadata } from "next";
import { Syne, Plus_Jakarta_Sans, JetBrains_Mono } from "next/font/google";
import "./globals.css";

const syne = Syne({
  subsets: ["latin"],
  weight: ["600", "700", "800"],
  variable: "--font-display",
});

const jakarta = Plus_Jakarta_Sans({
  subsets: ["latin"],
  weight: ["400", "500", "600", "700"],
  variable: "--font-body",
});

const jetbrains = JetBrains_Mono({
  subsets: ["latin"],
  weight: ["400", "500", "600"],
  variable: "--font-mono",
});

export const metadata: Metadata = {
  title: "SYNC SOUND — One sound. Everyone listening.",
  description:
    "Play any Windows 11 audio through multiple Bluetooth headphones and speakers simultaneously. Per-device rotary volume, instant Quick Sidebar, zero clutter.",
  icons: {
    icon: "/app-icon.png",
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html
      lang="en"
      className={`${syne.variable} ${jakarta.variable} ${jetbrains.variable}`}
    >
      <body className="bg-[#07090F] text-[#F8FAFC] antialiased selection:bg-[#22D3EE]/30 selection:text-[#F8FAFC]">
        {children}
      </body>
    </html>
  );
}
