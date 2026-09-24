import {
  type RenderMode,
  type ScaleformRenderConfiguration,
  type Target,
  TargetType,
  type UserInterfaceMediaPlayer,
} from "@hypnonema/generated-types";
import { ApiClient } from "../utils/apiClient.ts";

export class MediaPlayer {
  private readonly mediaPlayer: UserInterfaceMediaPlayer;
  private readonly apiClient: ApiClient;

  constructor(mediaPlayerData: UserInterfaceMediaPlayer) {
    this.mediaPlayer = mediaPlayerData;
    this.apiClient = new ApiClient(this.handle);
  }

  get isPlaying() {
    return this.hasTrack && !this.mediaPlayer.paused;
  }

  get isMuted() {
    return this.mediaPlayer.muted;
  }

  get isAdActive() {
    return this.mediaPlayer.adActive;
  }

  get isLooped() {
    return this.mediaPlayer.looped;
  }

  get isVideoEnabled() {
    return this.mediaPlayer.videoEnabled;
  }

  get hasTrack() {
    return (
      this.mediaPlayer.currentTrack !== undefined &&
      this.mediaPlayer.currentTrack.url !== ""
    );
  }

  get canChangeRenderMode() {
    return this.hasTrack && this.mediaPlayer.target.type !== TargetType.Screen;
  }

  get handle() {
    return this.mediaPlayer.handle;
  }

  get renderMode() {
    return this.mediaPlayer.renderMode;
  }

  get paused() {
    return this.mediaPlayer.paused;
  }

  get muted() {
    return this.mediaPlayer.muted;
  }

  get videoEnabled() {
    return this.mediaPlayer.videoEnabled;
  }

  get looped() {
    return this.mediaPlayer.looped;
  }

  get label() {
    return this.mediaPlayer.label;
  }

  get target() {
    return this.mediaPlayer.target;
  }

  get error() {
    return this.mediaPlayer.error ?? "";
  }

  get track() {
    return this.mediaPlayer.currentTrack;
  }

  get volume() {
    return this.mediaPlayer.volume;
  }

  get distance() {
    return Math.round(this.mediaPlayer.distance);
  }

  get scaleform() {
    return this.mediaPlayer.scaleform;
  }

  get canSaveScaleform() {
    return this.target.type === TargetType.Screen;
  }

  get queue() {
    return this.mediaPlayer.queue ?? [];
  }

  get isQueueEmpty() {
    return this.queue.length === 0;
  }

  get isStream() {
    return this.mediaPlayer.currentTrack?.duration === -1;
  }

  public stop() {
    this.apiClient.stop();
  }

  public setVolume(volume: number) {
    this.apiClient.setVolume(volume);
  }

  public skipNext() {
    this.apiClient.playNext();
  }

  public setScaleform(scaleform: ScaleformRenderConfiguration) {
    this.apiClient.setScaleform(scaleform);
  }

  public saveScaleform() {
    return this.apiClient.saveScaleform();
  }

  public setLooped(looped: boolean) {
    this.apiClient.setLooped(looped);
  }

  public setMuted(muted: boolean) {
    this.apiClient.setMuted(muted);
  }

  public setRenderMode(renderMode: RenderMode) {
    this.apiClient.setRenderMode(renderMode);
  }

  public setVideoEnabled(enabled: boolean) {
    this.apiClient.setVideoEnabled(enabled);
  }

  public setPaused(pause: boolean) {
    this.apiClient.setPaused(pause);
  }

  public play(
    url: string,
    title: string,
    thumbnailUrl: string,
    target: Target,
  ) {
    this.apiClient.play(url, title, thumbnailUrl, target);
  }

  public seek(position: number) {
    this.apiClient.seek(position);
  }
}
