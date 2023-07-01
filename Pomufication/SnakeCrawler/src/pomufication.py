import json
import logging
import re
import sys
import time
import argparse
from datetime import datetime
from shlex import split as shlex_split
from shutil import which

from apscheduler.schedulers.background import BackgroundScheduler

import browser
from scheduler import Scheduler

IS_DEBUG = True

STREAMLINK = which("streamlink")

INVALID_CHARS = ['/', '\\', '\0', '`', '*', '|', ':', ':', '&']
REGEX_CONVERSIONS = {0: 2, 1: 8, 4: 16, 5: 64, 8: 512}


class LoggingFormatter(logging.Formatter):
    # Colors
    black = "\x1b[30m"
    red = "\x1b[31m"
    green = "\x1b[32m"
    yellow = "\x1b[33m"
    blue = "\x1b[34m"
    gray = "\x1b[38m"
    # Styles
    reset = "\x1b[0m"
    bold = "\x1b[1m"

    COLORS = {
        logging.DEBUG: gray + bold,
        logging.INFO: blue + bold,
        logging.WARNING: yellow + bold,
        logging.ERROR: red,
        logging.CRITICAL: red + bold,
    }

    def format(self, record):
        log_color = self.COLORS[record.levelno]
        format = "(gray){asctime}(reset) (levelcolor){levelname:<8}(reset) \
            (green){name}(reset) {message}"
        format = format.replace("(gray)", self.gray + self.bold)
        format = format.replace("(reset)", self.reset)
        format = format.replace("(levelcolor)", log_color)
        format = format.replace("(green)", self.green + self.bold)
        formatter = logging.Formatter(format, "%Y-%m-%d %H:%M:%S", style="{")
        return formatter.format(record)

logger = logging.getLogger("Pomufication")
logger.setLevel(logging.INFO)

# Console handler
console_handler = logging.StreamHandler()
console_handler.setFormatter(LoggingFormatter())
# File handler
file_handler = logging.FileHandler(filename="pomufication.log", encoding="utf-8", mode="w")
file_handler_formatter = logging.Formatter(
    "[{asctime}] [{levelname:<8}] {name}: {message}", "%Y-%m-%d %H:%M:%S", style="{"
)
file_handler.setFormatter(file_handler_formatter)

# Add the handlers
logger.addHandler(console_handler)
logger.addHandler(file_handler)

class Info:
    def __init__(self, channel, title, video_id, start_date, is_live=False) -> None:
        self.channel:str = channel
        self.title:str = title
        self.video_id:str = video_id
        self.start_date:int = start_date
        self.is_live:bool = is_live


class Downloader:
    def __init__(self) -> None:
        self.scheduler = Scheduler(logger)
        self.active_downloads = {}
        self.active_ids = set()
        self.cfg = load_config()
        self.dl_path = self.cfg['DataDirectory']


    def check_channels(self):
        self.cfg = load_config()
        for channel in self.cfg['Channels']:
            if channel['Enabled'] is False:
                continue
            channel_id = channel["ChannelId"]
            filters = channel['FilterKeywords']
            self.check_single_channel(channel_id, filters)


    def parse_usr_link(self, _link):
        logger.info("Parsing user link from arg")

        page = browser.fetch_yt_page(_link)

        is_live, title, video_id, ch_name, start_date = browser.get_live_page_info(page)

        if self.scheduler.check_if_id_exists(video_id):
            logger.info("Already downloading %s - %s", ch_name, title)
            return

        if is_live:
            logger.info("Found a currently live stream! %s - %s", ch_name, title)
        else:
            logger.info("Found a scheduled live stream! %s - %s -> %s", ch_name, title, \
                  datetime.fromtimestamp(start_date))

        self.streamlink(ch_name, title, video_id, start_date)


    def checks_helper(self, info: Info, filters:list):
        if not self.filter_stream(info.title, filters):
            logger.info("Skipping %s - %s since not in filter.", info.channel, info.title)
            return
        if self.scheduler.check_if_id_exists(info.video_id):
            logger.info("Already downloading %s", info.channel)
            return

        if info.is_live:
            logger.info("Found a currently live stream! %s - %s", info.channel, info.title)
        else:
            logger.info("Found a scheduled live stream! %s - %s -> %s", info.channel, info.title, \
                  datetime.fromtimestamp(info.start_date))

        self.streamlink(info.channel, info.title, info.video_id, info.start_date)

    def check_single_channel(self, ch_id:str, filters: list) -> None:

        channel_page = browser.fetch_channel_page(f"https://www.youtube.com/channel/{ch_id}/")
        channel_name, resp = browser.get_channel_info(channel_page)

        logger.info("Checking %s", channel_name)
        if resp is None:
            live_page = browser.fetch_yt_page(f"https://www.youtube.com/channel/{ch_id}/live")
            live_link = browser.find_live_url(live_page)

            if live_link is False:
                logger.info("No live stream found for %s", channel_name)
                return

            live_page = browser.fetch_yt_page(live_link)
            is_live, title, video_id, _, start_date = browser.get_live_page_info(live_page)

            info = Info(channel_name, title, video_id, start_date, is_live)
            self.checks_helper(info, filters)

        else:
            # elem is title, video_id, start_date
            for elem in resp:
                info = Info(channel_name, elem[0], elem[1], elem[2])
                self.checks_helper(info, filters)


    def filter_stream(self, title, filters):
        for _f in filters:
            if not _f['Enabled']:
                continue
            res = self.filter_stream_helper(_f, title)
            if res is True:
                return True
        return False


    def filter_stream_helper(self, _filter, title):
        _type = _filter['Type']
        if _type == 0:
            return match_words(title, _filter['Filters'], _filter["Comparison"])
        return match_regex(title, _filter['Filters'], _filter["RegexOptions"])


    def streamlink(self, channel, title, video_id, startTimne):
        logger.info("Queuing streamlink child process for %s", channel)
        fname = sanitize(f"{channel} - {title}")
        link = f"https://www.youtube.com/watch?v={video_id}"
        cmd = f"{STREAMLINK} {link} best --stream-segment-timeout 60 \
                --player-external-http-continuous no --stream-timeout 360 \
                --retry-streams 30 -o \"{self.dl_path}/{fname}.mkv\""

        self.scheduler.create_process_order(video_id, startTimne, shlex_split(cmd), fname)

def match_words(name: str, words: list, comparison: int) -> bool:
    if comparison % 2 == 1:
        flag_options = re.IGNORECASE
    else:
        flag_options = re.NOFLAG
    for pat in words:
        _ret = re.findall(pat, name, flags=flag_options)
        if len(_ret) > 0:
            return True
    return False

def match_regex(name: str, regex: list, options: int) -> bool:
    flag_options = get_flags(options)
    for pat in regex:
        _ret = re.search(pat, name, flags=flag_options)
        if _ret is not None:
            return True
    return False

def get_flags(options: int) -> int:
    new_flag = 0
    bits = f"{options:09b}"[::-1]
    keys = REGEX_CONVERSIONS.keys()
    for idx, bit in enumerate(bits):
        if bit == '1' and idx in keys:
            new_flag += REGEX_CONVERSIONS[idx]

    return new_flag

def sanitize(value: str) -> str:
    for sub in INVALID_CHARS:
        value = value.replace(sub, '')
    m = re.match(r"^-+(.*)", value)
    if m:
        return m.group(1)
    return value

def load_config() -> dict :
    path = "config.json"
    with open(path, "r", encoding="utf-8") as cfg:
        data = json.load(cfg)
        return data


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('-l', action='store', required=False, help='Adds link to queue')
    args = parser.parse_args()


    app = Downloader()
    scheduler = BackgroundScheduler()
    scheduler.add_job(app.check_channels, 'interval', minutes=30)
    scheduler.add_job(app.scheduler.check_childs, 'interval', minutes=5)
    scheduler.start()
    logger.info("Started pomufication.")

    try:
        app.check_channels()
        if args.l:
            app.parse_usr_link(args.l)
        while True:
            time.sleep(100)

    except( KeyboardInterrupt, SystemExit):
        pass
    except Exception as e:
        logger.exception("Error occured: %s", e)
    finally:
        logger.info("Shutting down")
        for process in app.active_downloads.values():
            process.kill()
        scheduler.shutdown()
        sys.exit(0)