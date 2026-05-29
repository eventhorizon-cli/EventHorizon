# Event Horizon Workbench

React + TypeScript + Vite frontend for the AGUI-first EventHorizon experience.

## Scripts

```bash
npm install
npm run dev
npm run build
npm run lint
npm run test
```

## Local development

```bash
cd eventhorizon-workbench
npm install
npm run dev
```

## Local build

```bash
cd eventhorizon-workbench
npm run build
```

The local production build is emitted to `eventhorizon-workbench/dist`.

Local frontend builds do not write directly into `src/EventHorizon/wwwroot`.

GitHub Actions or a packaging script is responsible for copying `dist` into `src/EventHorizon/wwwroot` before building and packing the `.NET tool`.

