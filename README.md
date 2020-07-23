# Transcoder
Cross-plataform ffmpeg transcoder. It would transcode two videos in parallel using NVIDIA hardware acceleration.


### Usage

- Just enter the path of the video/s when asked.

#### Default ffmpeg command
```cmd
ffmpeg  ffmpeg -vsync 0 -hwaccel cuvid -c:v h264_cuvid -i video -c:v hevc_nvenc -x265-params crf=20 -spatial_aq 1 -rc-lookahead 20 -preset slow -c:a aac -b:a 224k -map 0 video-trans.mkv
```
