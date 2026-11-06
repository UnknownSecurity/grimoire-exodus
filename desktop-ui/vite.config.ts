import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import { resolve } from "node:path";

export default defineConfig({
  base: "./",
  plugins: [vue()],
  build: {
    outDir: resolve(__dirname, "../app/Resources/Shell"),
    emptyOutDir: true,
    sourcemap: false,
    assetsDir: "assets"
  }
});
