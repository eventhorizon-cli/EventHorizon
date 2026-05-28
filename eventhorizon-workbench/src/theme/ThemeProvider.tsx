import { useEffect, type PropsWithChildren } from "react";
import { useWorkbenchStore } from "@/store/workbenchStore";

export function ThemeProvider({ children }: PropsWithChildren) {
  const themeMode = useWorkbenchStore((state) => state.themeMode);

  useEffect(() => {
    const root = document.documentElement;
    const mediaQuery = window.matchMedia("(prefers-color-scheme: dark)");

    const applyTheme = () => {
      const resolved = themeMode === "system" ? (mediaQuery.matches ? "dark" : "light") : themeMode;
      root.classList.toggle("dark", resolved === "dark");
    };

    applyTheme();
    mediaQuery.addEventListener("change", applyTheme);
    return () => mediaQuery.removeEventListener("change", applyTheme);
  }, [themeMode]);

  return children;
}

