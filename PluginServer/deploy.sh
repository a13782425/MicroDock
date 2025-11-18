#!/bin/bash

# MicroDock Plugin Server 部署脚本
# 使用方法: ./deploy.sh [start|stop|restart|logs|status]

set -e

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 应用信息
APP_NAME="MicroDock Plugin Server"
DOCKER_COMPOSE_FILE="docker-compose.yml"

# 日志函数
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# 检查Docker和Docker Compose
check_dependencies() {
    log_info "检查系统依赖..."

    if ! command -v docker &> /dev/null; then
        log_error "Docker 未安装，请先安装 Docker"
        exit 1
    fi

    if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
        log_error "Docker Compose 未安装，请先安装 Docker Compose"
        exit 1
    fi

    log_success "系统依赖检查完成"
}

# 检查环境文件
check_env_file() {
    if [ ! -f ".env" ]; then
        log_warning ".env 文件不存在，正在从 .env.example 复制..."
        if [ -f ".env.example" ]; then
            cp .env.example .env
            log_warning "请编辑 .env 文件配置您的环境变量"
        else
            log_error ".env.example 文件不存在"
            exit 1
        fi
    fi
}

# 创建必要的目录
create_directories() {
    log_info "创建必要的目录..."
    mkdir -p data/plugins data/backups data/uploads logs
    chmod 755 data data/plugins data/backups data/uploads
    log_success "目录创建完成"
}

# 启动服务
start_services() {
    log_info "启动 $APP_NAME..."

    check_dependencies
    check_env_file
    create_directories

    # 构建并启动服务
    if command -v docker-compose &> /dev/null; then
        docker-compose -f $DOCKER_COMPOSE_FILE up -d --build
    else
        docker compose -f $DOCKER_COMPOSE_FILE up -d --build
    fi

    log_success "$APP_NAME 启动完成"
    show_service_info
}

# 停止服务
stop_services() {
    log_info "停止 $APP_NAME..."

    if command -v docker-compose &> /dev/null; then
        docker-compose -f $DOCKER_COMPOSE_FILE down
    else
        docker compose -f $DOCKER_COMPOSE_FILE down
    fi

    log_success "$APP_NAME 已停止"
}

# 重启服务
restart_services() {
    log_info "重启 $APP_NAME..."
    stop_services
    sleep 2
    start_services
}

# 查看日志
show_logs() {
    log_info "显示服务日志..."

    if [ -n "$1" ] && [ "$1" = "follow" ]; then
        if command -v docker-compose &> /dev/null; then
            docker-compose -f $DOCKER_COMPOSE_FILE logs -f
        else
            docker compose -f $DOCKER_COMPOSE_FILE logs -f
        fi
    else
        if command -v docker-compose &> /dev/null; then
            docker-compose -f $DOCKER_COMPOSE_FILE logs --tail=100
        else
            docker compose -f $DOCKER_COMPOSE_FILE logs --tail=100
        fi
    fi
}

# 查看服务状态
show_status() {
    log_info "检查服务状态..."

    if command -v docker-compose &> /dev/null; then
        docker-compose -f $DOCKER_COMPOSE_FILE ps
    else
        docker compose -f $DOCKER_COMPOSE_FILE ps
    fi
}

# 显示服务信息
show_service_info() {
    echo ""
    log_success "🎉 $APP_NAME 部署成功!"
    echo ""
    echo "服务访问地址:"
    echo "  📱 前端界面: http://localhost:3000"
    echo "  🔌 API接口: http://localhost:8000"
    echo "  📚 API文档: http://localhost:8000/api/docs"
    echo ""
    echo "管理命令:"
    echo "  查看日志: ./deploy.sh logs"
    echo "  跟踪日志: ./deploy.sh logs follow"
    echo "  查看状态: ./deploy.sh status"
    echo "  重启服务: ./deploy.sh restart"
    echo "  停止服务: ./deploy.sh stop"
    echo ""
    echo "数据目录:"
    echo "  插件目录: ./data/plugins"
    echo "  备份目录: ./data/backups"
    echo "  上传目录: ./data/uploads"
    echo ""
}

# 清理资源
cleanup() {
    log_info "清理 Docker 资源..."

    if command -v docker-compose &> /dev/null; then
        docker-compose -f $DOCKER_COMPOSE_FILE down -v --remove-orphans
        docker system prune -f
    else
        docker compose -f $DOCKER_COMPOSE_FILE down -v --remove-orphans
        docker system prune -f
    fi

    log_success "清理完成"
}

# 显示帮助信息
show_help() {
    echo "MicroDock Plugin Server 部署脚本"
    echo ""
    echo "使用方法:"
    echo "  $0 [命令]"
    echo ""
    echo "可用命令:"
    echo "  start     启动服务 (默认)"
    echo "  stop      停止服务"
    echo "  restart   重启服务"
    echo "  logs      查看服务日志"
    echo "  logs follow  跟踪服务日志"
    echo "  status    查看服务状态"
    echo "  cleanup   清理 Docker 资源"
    echo "  help      显示帮助信息"
    echo ""
}

# 主函数
main() {
    local command=${1:-start}

    case "$command" in
        "start")
            start_services
            ;;
        "stop")
            stop_services
            ;;
        "restart")
            restart_services
            ;;
        "logs")
            show_logs "$2"
            ;;
        "status")
            show_status
            ;;
        "cleanup")
            cleanup
            ;;
        "help"|"-h"|"--help")
            show_help
            ;;
        *)
            log_error "未知命令: $command"
            show_help
            exit 1
            ;;
    esac
}

# 捕获信号
trap 'log_warning "脚本被中断"; exit 1' INT TERM

# 执行主函数
main "$@"