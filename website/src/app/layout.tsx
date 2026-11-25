import type { Metadata } from "next";
import { Instrument_Serif, Plus_Jakarta_Sans, JetBrains_Mono } from "next/font/google";
import "./globals.css";

const serif = Instrument_Serif({
  subsets: ["latin"],
  weight: ["400"],
  style: ["normal", "italic"],
  variable: "--font-serif",
});

const jakarta = Plus_Jakarta_Sans({
  subsets: ["latin"],
  weight: ["400", "500", "600", "700", "800"],
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
    "Windows only plays to one pair of headphones. SYNC SOUND plays to everyone. Per-headphone rotary volume, Ctrl+Shift+S Quick Sidebar, zero clutter.",
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
      className={`${serif.variable} ${jakarta.variable} ${jetbrains.variable}`}
    >
      <body className="bg-[#F7F4ED] text-[#141417] antialiased selection:bg-[#E85D3F]/20 selection:text-[#141417]">
        {children}
      </body>
    </html>
  );
}
