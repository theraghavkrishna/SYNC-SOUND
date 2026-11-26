import type { Metadata } from "next";
import { Instrument_Serif, Manrope, JetBrains_Mono } from "next/font/google";
import "./globals.css";

const serif = Instrument_Serif({
  subsets: ["latin"],
  weight: ["400"],
  style: ["normal", "italic"],
  variable: "--font-serif",
});

const manrope = Manrope({
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
    "Share your Windows PC's audio across multiple supported headphones. Choose who's listening. Press play.",
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
      className={`${serif.variable} ${manrope.variable} ${jetbrains.variable}`}
    >
      <body className="bg-[#F4EFE6] text-[#191817] antialiased selection:bg-[#C65D32]/20 selection:text-[#191817]">
        {children}
      </body>
    </html>
  );
}
