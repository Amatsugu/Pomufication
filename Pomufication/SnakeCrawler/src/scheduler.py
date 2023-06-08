from datetime import datetime, timedelta
from subprocess import Popen, DEVNULL


START_CHILD_TIME = timedelta(minutes=10)

class Info:
    def __init__(self, title: str, spawn_cmd: list, startTime: (int | None) = None) -> None:
        if startTime:
            self.startTime = datetime.fromtimestamp(startTime)
        else:
            self.startTime = None
        self.title = title
        self.spawn_cmd = spawn_cmd
        self.process = None


    def run(self):
        with Popen(self.spawn_cmd,stdout=DEVNULL) as child:
            self.process = child

class Scheduler:
    def __init__(self, logger) -> None:
        self.queue = {}
        self.child_ids = []
        self.logger = logger


    def check_childs(self) -> None:
        to_kill = set()
        for child_id in self.child_ids:
            if self.queue[child_id].process is not None:
                if self.queue[child_id].process.poll() is not None:
                    to_kill.add(child_id)
                continue

            if self.queue[child_id].startTime is None:
                should_start = True
            else:
                should_start = (self.queue[child_id].startTime - datetime.now()) < START_CHILD_TIME

            if should_start:
                self.logger.info("Spawning %s", self.queue[child_id].title)
                self.queue[child_id].run()

        for _id in to_kill:
            self.logger.info("Killing %s", self.queue[_id].title)
            self.kill_child_process(_id)


    def create_process_order(self, video_id: str, startTime: datetime | None, \
                              spawn_cmd: list, title: str) -> None:
        child_info = Info(title, spawn_cmd, startTime)
        self.queue[video_id] = child_info
        self.child_ids.append(video_id)
        self.check_childs()

    def check_if_id_exists(self, video_id: str) -> bool:
        return video_id in self.child_ids

    def kill_child_process(self, child_id):
        self.queue[child_id].process.kill()
        self.queue.pop(child_id)
        self.child_ids.remove(child_id)
