import './AdOverlay.css';

interface AdOverlayProps {
	active: boolean;
	thumbnailUrl?: string;
}

export const AdOverlay = ({ active, thumbnailUrl }: AdOverlayProps) => {
	return (
		<div className={`ad-overlay${active ? ' active' : ''}`}>
			{thumbnailUrl && <img className="ad-overlay__thumbnail" src={thumbnailUrl} alt="" />}
			<div className="ad-overlay__scrim" />
			<div className="ad-overlay__content">
				<div className="ad-overlay__badge">
					<span className="ad-overlay__badge-dot" />
					Advertisement
				</div>
				<p className="ad-overlay__title">Ad break</p>
				<p className="ad-overlay__subtitle">Playback will resume automatically</p>
				<div className="ad-overlay__dots">
					<span />
					<span />
					<span />
				</div>
			</div>
		</div>
	);
};
