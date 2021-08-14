# MyLab.PrometheusAgent
[![Docker image](https://img.shields.io/docker/v/mylabtools/prometheus-agent?label=Docker%20image)](https://hub.docker.com/r/mylabtools/prometheus-agent)

Ознакомьтесь с последними изменениями в [журнале изменений](/changelog.md).

## Обзор 

Собирает, аггрегирует и предоставляет метрики других сервисов в формате [Prometheus](https://prometheus.io/docs/concepts/data_model/). 

Метрики собираются в момент запроса параллельно со всех целевых сервисов.

## Производительность

`MyLab.PrometheusAgent` является частичной альтернативой сервиса [Prometheus](https://prometheus.io/), развёрнутого с целью использования в [федеративном режиме](https://prometheus.io/docs/prometheus/latest/federation/). Поэтому в данном разделе сравнение происходит с ним.

Тестовое окружение:

* RAM: **8 Gb**
* CPU: **2х Intel(R) Xeon(R) CPU E5-2630 v4 @ 2.20GHz**
* Целевых сервисов: **75**

| Решение                 | Потребление памяти | Потребление процессора |
| ----------------------- | ------------------ | ---------------------- |
| `MyLab.PrometheusAgent` | ~400 Mb            | ~2%                    |
| `Prometheus`            | ~1.16 Gb           | ~20%                   |

## API

### GET /metrics

Возвращает собранные метрики.

Пример:

```
# TYPE bar_metric counter
bar_metric {label3="value3",label4="value4",target_batch="1",instance="localhost:10201",job="job1"} 2.20 1624868358000
# TYPE foo_metric gauge
foo_metric {label1="value1",label2="value2",target_batch="1",instance="localhost:10200",job="job1"} 1.10
# TYPE bar_metric counter
bar_metric {label3="value3",label4="value4",target_batch="2",instance="localhost:10201",job="job1"} 2.20 1624868358000
# TYPE foo_metric gauge
foo_metric {label1="value1",label2="value2",target_batch="2",instance="localhost:10200",job="job1"} 1.10
# TYPE bar_metric counter
bar_metric {label3="value3",label4="value4",target_batch="1",instance="localhost:10201",job="job2"} 2.20 1624868358000
# TYPE foo_metric gauge
foo_metric {label1="value1",label2="value2",target_batch="1",instance="localhost:10200",job="job2"} 1.10
```

### GET /config

Возвращает конфигурацию сбора метрик в `JSON` представлении.

Пример:

```json
{
  "Items": [
    {
      "JobName": "job1",
      "StaticConfigs": [
        {
          "Targets": [
            "localhost:10200",
            "localhost:10201"
          ],
          "Labels": {
            "target_batch": "1"
          }
        }
      ]
    }
  ]
}
```

### GET /report

Возвращает отчёт об опросах целевых сервисов.

Пример:

```json
[
  {
    "Id": "localhost:10201",
    "Dt": "2021-06-29T12:13:29.0932695+03:00",
    "Duration": "00:00:00.0113103",
    "Error": null,
    "ResponseVolume": 87,
    "MetricsCount": 1
  },
  {
    "Id": "localhost:10200",
    "Dt": "2021-06-29T12:13:29.0435334+03:00",
    "Duration": "00:00:00.0596145",
    "Error": null,
    "ResponseVolume": 71,
    "MetricsCount": 1
  }
]
```

## Конфигурация

Конфигурирование сервиса осуществляется в соответствии с [правилами конифгурации .NET 5 приложения](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-5.0) и [конфигурацией при использовании хоста по умолчанию .NET](https://docs.microsoft.com/en-us/dotnet/core/extensions/generic-host#default-builder-settings).

Конфигурация загружается из узла `PrometheusAgent`. Её струткра:

* `ScrapeConfig` - путь к файлу с настройками получения метрик;
* `ConfigExpyrySec` - период актуальности загруженной конфигурации получения метрик (sec). По истечении это времени, `PrometheusAgent` заново загрузит эту конфигурацию;
* `ScrapeTimeoutSec` - таймаут запроса метрик у целевого сервиса (sec);
* `Docker` - настройки обнаружения целевых сервисов из `Docker`:
  * `Strategy` - [стратегия обнаружения](#Стратегии-обнаружения). `None` - по умолчанию.
  * `Socket` - путь к `docker`-сокету. По умолчанию: `unix:///var/run/docker.sock`;
  * `Labels` - именованные дополнительные метки для всех контейнеров;
  * `DisableServiceContainerLabels` - флаг, указывающий на исключение служеюных меток из меток контенйера.

### Docker Discovery

Для актуивации включения поиска по контенйнерам локального хоста, необходимо:

* в конфигурации указать стратегию обнаружения `PrometheusAgent__Docker__Strategy`;
* при развёртывании подключить файл сокета в контенйер `PrometheusAgent`.

#### Стратегии обнаружения

##### Стартегиая обнаружения `ALL`

Принцип: `все работающие, кроме отмеченных`.

При этой стратегии обнаруженияпринимаются все контейнеры, удовлетворяющие всем следующим условиям:

* контейнер находится в статусе `running`;
* контенйер снабжён меткой `metrics_exclude` со значением `true`.

##### Стартегиая обнаружения `Include`

Принцип: `только отмеченные`.

При этой стратегии обнаружения принимаются все контейнеры, удовлетворяющие всем следующим условиям:

* контейнер находится в статусе `running`;
* контенйер снабжён меткой `metrics_include` со значением `true`.

#### Метки обнаружения

`PrometheusAgent` поддерживает метки контейнеров для влияния на процесс обнаружения:

* `metrics_exclude` - если значение `true`, исключает контенер из обнаружения, если стратегия обнаружения `All`;
* `metrics_include` - если значение `true`, включает контенер в результаты обнаружения, если стратегия обнаружения `include`;
* `metrics_port` - определяет порт подключеения, по которому можно получить метрики контенйера. `80` - по умолчанию;
* `metrics_path` - определяет путь запроса, по которому можно получить метрики контейнера. `/metrics` - по умолчанию.

Метки контейнера, начинающиеся с `metrics_`, рассматриваются, применются к мерикам, как метки с именем без этого префикса. Например, метка контенера `metric_host: dev-app.corp`, при этом к метрикам контейнера будет добавлена метка `host: "dev-app.corp".`  

Все остальные метки контенера будут добавлены к метрикам контейнера с префиксом в имени `container_label_`. При этом само имя метки будет нормализовано путём замены всех символов, кроме разрешённых (буквы, цифры, знаки `_`и `:` ), на `_`. Например, метка контенера `target.host: dev-app.corp`, при этом к метрикам контейнера будет добавлена метка `container_label_target_host: "dev-app.corp".`

#### Служебные метки

`PrometheusAgent` поддерживает исключение служебных метрик `Docker`. 

По умолчанию, этот механизм активен. Отключение происхоит через конфигурацию: `PrometheusAgent__Docker__DisableServiceContainerLabels = false`

Исключению подвергаются метки:

* `maintainer`
* `com.docker.compose.*`
* `desktop.docker.*`

### File Discovery

Конфигурация сборки метрик через файл. Путь к файлу с настройками задаётся конфигурацией `PrometheusAgent__ScrapeConfig`.

Формат файла соответствует урезанной спецификации конфигурационного файла `Prometheus` в части [scrape_config](https://prometheus.io/docs/prometheus/latest/configuration/configuration/#scrape_config).

Поддерживаемые узлы:

| Путь                                      | Описание                                                     |
| ----------------------------------------- | ------------------------------------------------------------ |
| scrape_configs                            | Секции, описывающие работы по сборке метрик: подключения и параметры |
| scrape_configs[]/job_name                 | Наименование работы, ассоциированное со сборкой метрик       |
| scrape_configs[]/metrics_path             | Путь запроса к метркикам                                     |
| scrape_configs[]/static_configs           | Указывает список подключений и меток                         |
| scrape_configs[]/static_configs[]/targets | Целевые подключения (хост+порт)                              |
| scrape_configs[]/static_configs[]/labels  | Метки, которые будут добавляться ко всем метрикам, полученным из указанных в работе целевых подключений |

Пример конфигурационного файла:

```yaml
scrape_configs:

- job_name: job1
  static_configs:
  - targets:
    - localhost:10200
    - localhost:10201
    labels:
      target_batch: 1
  - targets:
    - localhost:10200
    - localhost:10201
    labels:
      target_batch: 2
      
- job_name: job2
  static_configs:
  - targets:
    - localhost:10200
    - localhost:10201
    labels:
      target_batch: 1

```

## Развёртывание

Сервис адаптирован для развёртывания на `docker`. 

Пример `docker-compose` файла при использовании файла конфигурации:

```yaml
version: '3.2'

services:
  prometheus-agent:
    image: mylabtools/prometheus-agent:latest
    container_name: prometheus-agent
    volumes:
    - ./scrape_config.yml:/scrape_config.yml
    environment:
    - PrometheusAgent__ScrapeConfig=/scrape_config.yml
    
```

Пример `docker-compose` файла при использовании `docker`-обнаружения:

```yaml
version: '3.2'

services:
  prometheus-agent:
    image: mylabtools/prometheus-agent:latest
    container_name: prometheus-agent
    volumes:
    - /var/run/docker.sock:/var/run/docker.sock
    environment:
    - PrometheusAgent__Docker__Strategy=All
    - PrometheusAgent__Docker__Labels__host=dev-app.corp
    - PrometheusAgent__ConfigExpirySec=60
```

## Рекурсия сбора метрик

`PrometheusAgent` из образа уже помечен меткой `metrics_exclude: true`. Это исключает сбор метрик с самого себя и возникновение рекурсии. 
