import {
  NuiEvents,
  type RenderMode,
  type ScaleformRenderConfiguration,
  type Target,
} from "@hypnonema/generated-types";
import { post } from "./misc.ts";

export class ApiClient {
  private readonly handle: number;

  constructor(handle: number) {
    this.handle = handle;
  }

  public async play(
    url: string,
    title: string,
    thumbnailUrl: string,
    target: Target,
  ): Promise<void> {
    // Intentionally bypasses send(): the Play event is global and must not be scoped.
    // TODO: Unify the API so both scoped and global events use the same sending mechanism.
    await post(
      NuiEvents.play,
      this.buildPayload({
        track: { url, title, thumbnailUrl },
        target: target,
      }),
    );
  }

  private getScopedEvent(event: string): string {
    return `mediaPlayer:${this.handle}:${event}`;
  }

  private async send<T extends object>(
    event: string,
    payload?: T,
  ): Promise<void> {
    await post(this.getScopedEvent(event), this.buildPayload(payload));
  }

  public async stop() {
    await this.send(NuiEvents.stop);
  }

  public async seek(position: number) {
    await this.send(NuiEvents.seek, { position });
  }

  public async setVolume(volume: number) {
    await this.send(NuiEvents.setVolume, { volume });
  }

  public async playNext() {
    await this.send(NuiEvents.playNext);
  }

  public async setLooped(looped: boolean) {
    await this.send(NuiEvents.setLooped, { looped });
  }

  public async setPaused(paused: boolean) {
    await this.send(NuiEvents.setPaused, { paused });
  }

  public async setVideoEnabled(videoEnabled: boolean) {
    await this.send(NuiEvents.setVideoEnabled, { videoEnabled });
  }

  public async setMuted(muted: boolean) {
    await this.send(NuiEvents.setMuted, { muted });
  }

  public async setRenderMode(renderMode: RenderMode) {
    await this.send(NuiEvents.setRenderMode, { renderMode });
  }

  public async setScaleform(scaleform: ScaleformRenderConfiguration) {
    await this.send(NuiEvents.setScaleformOptions, {
      scaleform: scaleform,
    });
  }

  public async saveScaleform() {
    await this.send(NuiEvents.saveScaleformSettings);
  }

  private buildPayload<T extends object>(payload?: T) {
    return {
      handle: this.handle,
      ...payload,
    };
  }
}
