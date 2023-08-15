from datetime import datetime, timedelta
from shlex import split as cmd_split
from subprocess import DEVNULL, Popen
from shutil import which

from browser import fetch_yt_page, get_live_page_info

STREAMLINK_PATH = which("streamlink")
STREAMLINK_CMD = "STREAMLINK _LINK_ best --stream-segment-timeout 60 " + \
                 "--player-external-http-continuous no --stream-timeout 360 " + \
                 "--retry-streams 30 -o \"DL_PATH.mkv\""
YT_LINK = "https://youtube.com/watch?v="
START_CHILD_TIME = timedelta(minutes=10)

class Info:
    def __init__(self, title:str, video_id, path:str, startTime: (int | None) = None) -> None:
        if startTime:
            self.startTime = datetime.fromtimestamp(startTime)
        else:
            self.startTime = None
        self.title = title
        self.id = video_id
        self.link = YT_LINK+video_id
        self.spawn_cmd = STREAMLINK_CMD.replace('STREAMLINK', STREAMLINK_PATH) \
                                       .replace('_LINK_', self.link) \
                                       .replace('DL_PATH', path)
        self.spawn_cmd = cmd_split(self.spawn_cmd)
        self.process = None


    def run(self):
        with Popen(self.spawn_cmd, stdout=DEVNULL, stderr=DEVNULL) as child:
            self.process = child

class Scheduler:
    def __init__(self, logger) -> None:
        self.queue = {}
        self.child_ids = []
        self.logger = logger


    def check_start_time(self, child_id:str) -> None:
        child = self.queue[child_id]

        page = fetch_yt_page(child.link)
        _,_,_,_,start_time = get_live_page_info(page)
        start_time = datetime.fromtimestamp(start_time)

        if start_time != child.startTime:
            child.startTime = start_time
            self.queue[child.id] = child
            self.logger.info("Updated the time for %s", self.queue[child_id].title)


    def check_childs(self) -> None:
        to_kill = set()
        for child_id in self.child_ids:
            if self.queue[child_id].process is not None:
                if self.queue[child_id].process.poll() is not None:
                    to_kill.add(child_id)
                continue

            self.check_start_time(child_id)

            if self.queue[child_id].startTime == -1:
                should_start = True
            else:
                should_start = (self.queue[child_id].startTime - datetime.now()) < START_CHILD_TIME

            if should_start:
                self.logger.info("Spawning %s", self.queue[child_id].title)
                self.queue[child_id].run()

        for _id in to_kill:
            self.logger.info("Killing %s", self.queue[_id].title)
            self.kill_child_process(_id)


    def create_process_order(self, video_id: str, title: str,\
                             path:str, startTime: datetime | None) -> None:
        child_info = Info(title, video_id, path, startTime)
        self.queue[video_id] = child_info
        self.child_ids.append(video_id)
        self.check_childs()

    def check_if_id_exists(self, video_id: str) -> bool:
        return video_id in self.child_ids

    def kill_child_process(self, child_id):
        self.queue[child_id].process.kill()
        self.queue.pop(child_id)
        self.child_ids.remove(child_id)
