const oEmbedUrl = "https://noembed.com/embed?url={url}";

type OEmbedResponse = {
  thumbnail_url: string;
  title: string;
};

const transformThumbnailUrl = (url: string): string =>
  url.replace("height=100", "height=480").replace("-d_295x166", "-d_640");

export const getOEmbedFromURL = async (
  url: string,
): Promise<{ title: string; thumbnailUrl: string }> => {
  const response = await fetch(oEmbedUrl.replace("{url}", url));
  const data: OEmbedResponse = await response.json();

  const { thumbnail_url = "", title = "" } = data;

  const thumbnailUrl = thumbnail_url
    ? transformThumbnailUrl(thumbnail_url)
    : "";

  return { title, thumbnailUrl };
};
