import {
  type ITrack,
  type ModelTarget,
  RenderMode,
  type ScaleformRenderConfiguration,
  type ScreenTarget,
  TargetType,
  type UserInterfaceMediaPlayer,
} from "@hypnonema/generated-types";

const getRandomItem = <T>(arr: T[]): T =>
  arr[Math.floor(Math.random() * arr.length)];
const getRandomInt = ({ min = 0, max }: { min?: number; max: number }) =>
  Math.floor(Math.random() * (max - min + 1) + min);

const VIDEO_TRACKS: Array<Pick<ITrack, "url" | "thumbnailUrl" | "title">> = [
  {
    url: "https://youtu.be/UOol1wKtIVE",
    thumbnailUrl: "https://i.ytimg.com/vi/FQr7VrK5RRQ/hqdefault.jpg",
    title: "Train AI Models Offline on Your Own Machine",
  },
  {
    url: "https://youtu.be/UOol1wKtIVE",
    thumbnailUrl: "https://i.ytimg.com/vi/lTs6a0ORdQU/hqdefault.jpg",
    title: "The Warrior Song",
  },
  {
    url: "https://youtu.be/UOol1wKtIVE",
    thumbnailUrl: "https://i.ytimg.com/vi/5EWEkJA5GM0/hqdefault.jpg",
    title: "Imperial March played on pencil",
  },
  {
    url: "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4",
    title: "Local Test Video",
    thumbnailUrl: "",
  },
];

const TRACK_POSITIONS = [1, 50, 100];
const DEVICE_LABELS = [
  "TV",
  "Radio",
  "Boombox",
  "Jukebox",
  "Hypnonema",
  "Tablet",
];

const createTrack = (): ITrack => {
  const { url, thumbnailUrl, title } = getRandomItem(VIDEO_TRACKS);
  return {
    url,
    thumbnailUrl,
    duration: getRandomInt({ min: 100, max: 180 }),
    requestedBy: { handle: "1", name: "Random User" },
    position: getRandomItem(TRACK_POSITIONS),
    title,
  };
};

const createScaleform = (): ScaleformRenderConfiguration => ({
  transform: {
    position: { x: 100, y: 250, z: 20 },
    scale: { x: 0, y: 0, z: 0 },
    rotation: { x: 0, y: 0, z: 0 },
  },
  texture: {
    position: { x: 0, y: 0 },
    dimension: { width: 1280, height: 720 },
  },
});

const createMediaPlayer = (): UserInterfaceMediaPlayer => {
  const renderModes = [
    RenderMode.RenderTarget,
    RenderMode.Scaleform,
    RenderMode.ScaleformRenderTarget,
  ];

  const modelTarget: ModelTarget = {
    type: TargetType.Model,
    model: {
      prop: "prop_tv",
      label: "TV",
      renderTarget: getRandomItem([
        "tvscreen",
        "prop_hypnonema_01",
        "ex_laptop_display",
        "prop_hypnonema_02",
        "prop_hypnonema_03",
      ]),
    },
  };

  const screenTarget: ScreenTarget = {
    type: TargetType.Screen,
    screen: {
      name: "hypnonema",
      scaleform: createScaleform(),
      renderDistance: 200,
    },
  };

  return {
    handle: getRandomInt({ max: 100_000 }),
    label: getRandomItem(DEVICE_LABELS),
    distance: getRandomInt({ min: 1, max: 30 }),
    volume: getRandomInt({ min: 0, max: 100 }),
    currentTrack: Math.random() > 0.5 ? createTrack() : undefined,
    target: Math.random() > 0.5 ? modelTarget : screenTarget,
    muted: Math.random() > 0.5,
    paused: Math.random() > 0.5,
    renderMode: getRandomItem(renderModes),
    videoEnabled: Math.random() > 0.5,
    looped: Math.random() > 0.5,
    scaleform: createScaleform(),
    queue: Array.from({ length: 4 }, createTrack),
    error: Math.random() > 0.8 ? "Failed to load the url" : undefined,
  };
};

export const createRandomMediaPlayers = (
  count: number,
): UserInterfaceMediaPlayer[] =>
  Array.from({ length: count }, createMediaPlayer);
