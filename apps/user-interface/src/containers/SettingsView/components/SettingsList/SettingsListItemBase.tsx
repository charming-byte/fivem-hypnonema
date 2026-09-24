import type { FC, ReactNode } from "react";
import { Box, Card, CardContent, Typography } from "@mui/material";

export interface BaseSettingItemProps {
  title: string;
  helperText?: string;
  children?: ReactNode;
}

export const SettingListItemBase: FC<BaseSettingItemProps> = ({
  title,
  helperText,
  children,
}) => {
  return (
    <Box sx={{ width: "100%" }}>
      <Card
        sx={{
          borderRadius: 0,
          bgcolor: "#1e1e1e",
          color: "#fff",
          width: "100%",
        }}
      >
        <CardContent>
          <Box
            display="flex"
            justifyContent="space-between"
            alignItems="center"
          >
            <Box>
              <Typography
                variant="subtitle1"
                style={{
                  fontSize: "1.25rem",
                  letterSpacing: "0.025em",
                  textTransform: "uppercase",
                  userSelect: "none",
                }}
                fontWeight="100"
              >
                {title}
              </Typography>
              {helperText && (
                <Typography
                  variant="body2"
                  fontWeight="300"
                  style={{
                    fontSize: "0.95rem",
                    userSelect: "none",
                    color: "rgba(255, 255, 255, 0.7)",
                  }}
                >
                  {helperText}
                </Typography>
              )}
            </Box>

            {children}
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
};
